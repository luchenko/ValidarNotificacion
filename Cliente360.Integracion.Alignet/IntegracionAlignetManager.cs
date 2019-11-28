using BO.Integracion.Siebel;
using Cliente360.Integracion.Alignet.Entities;
using Release.Helper.Data.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace Cliente360.Integracion.Alignet
{
    public class IntegracionAlignetManager
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static string csAlignet;
        private readonly string APICREATEMAIL = ConfigurationManager.AppSettings["APICREATEMAIL"];
        private readonly string APICONSULTA = ConfigurationManager.AppSettings["APICONSULTA"];

        private readonly string APIREVERSE = ConfigurationManager.AppSettings["APIREVERSE"];

        private readonly string IDACQUIRER = ConfigurationManager.AppSettings["IDACQUIRER"];
        private readonly string IDCOMMERCE = ConfigurationManager.AppSettings["IDCOMMERCE"];
        //private string OPERATIONNUMBER = "62170007";
        private readonly string AUTHORIZATION = ConfigurationManager.AppSettings["AUTHORIZATION"]; 

        private static readonly string CUSTOMERKEY = ConfigurationManager.AppSettings["CUSTOMERKEY"];
        private static readonly string IDCOMMERCEMAIL = ConfigurationManager.AppSettings["IDCOMMERCEMAIL"];
        //System.Configuration.ConfigurationManager
        private static readonly string CODEST_PENDIENTEEXTORNO =  ConfigurationManager.AppSettings["CODEST_PENDIENTEEXTORNO"]; //3
        private static readonly string CODERROR_EXCEPCION =  ConfigurationManager.AppSettings["CODERROR_ALIGNET"]; //4
        private static readonly string CODERROR_ALIGNET =  ConfigurationManager.AppSettings["CODERROR_ALIGNET"]; //5
        private static readonly string CODEST_OKALIGNET =  ConfigurationManager.AppSettings["CODEST_OKALIGNET"]; //9

        public static HttpClient ApiClient { get; set; } 

        public IntegracionAlignetManager(string csalignet)
        {
            csAlignet = csalignet;
        }
        public void Start()
        {
            ApiClient = new HttpClient();
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            string _result;
            JObject objreversa;
            try
            {
                _logger.Info("Inicio del procesamiento API alignet...");
                var transacciones_alignet = ObtenerTransaccionesAlignet(CODEST_PENDIENTEEXTORNO);
                foreach (base_transacciones el in transacciones_alignet)
                {
                    Console.WriteLine(el.EMAIL);
                    //ConsultarAlignet("62170007");
                    _result = ReverseAlignet(el.NUMERO_PEDIDO);

                    if (_result.Length > 0)
                    {
                        objreversa = JObject.Parse(_result);
                        objreversa.TryGetValue("success", out JToken value);
                        Console.WriteLine(value.ToString());
                        if (value.ToString() == "true")
                        {
                            ActualizarTransaccionesAlignet(el.NUMERO_PEDIDO, CODEST_OKALIGNET);
                            StringContent xmlDoc = getXmlDoc(el.TITULAR, el.PAN, el.FECHA_PEDIDO, el.MONTO, el.ESTADO_OPERACION.ToString(), el.CODIGO_AUTORIZACION, el.BANCO, el.MARCA_TARJETA, el.EMAIL);
                            SendMailByApi(APICREATEMAIL, xmlDoc);
                        }
                        else
                        {
                            JToken objex = objreversa["Exception"];
                            if (objex != null)
                            {
                                ActualizarTransaccionesAlignet(el.NUMERO_PEDIDO, CODERROR_EXCEPCION);
                                //Console.WriteLine(objex.ToString());
                            }
                            else
                            {
                                objreversa.TryGetValue("message_ilgn", out JToken message);
                                ActualizarTransaccionesAlignet(el.NUMERO_PEDIDO, CODERROR_ALIGNET);
                                //Console.WriteLine(message.ToString());
                            }
                        }
                    }
                    else {
                        ActualizarTransaccionesAlignet(el.NUMERO_PEDIDO, CODERROR_ALIGNET);
                    }

                }

                _logger.Info("Fin del procesamiento API alignet...");
            }
            catch (Exception ex )
            {
                _logger.Error(ex);
            }
        }

        private List<base_transacciones> ObtenerTransaccionesAlignet(string estado)
        {
            _logger.Info("Inicio de la carga de transacciones alignet...");
            string sql = "Select * from base_transacciones_alignet where ESTADO_OPERACION = '"+ estado + "'"; //"dbo.getBaseAlignet @ESTADO_OPERACION = 3;"; 
            var transacciones = GetData(sql, CommandType.Text);
            _logger.Info(string.Format("Se obtuvieron {0} transacciones...", transacciones.Count));
            return transacciones;
        }

        public void ActualizarTransaccionesAlignet(string operationNumber,string estado_operacion)
        {
            _logger.Info("Actualiza carga de transacciones alignet...");
            string sql = "UPDATE base_transacciones_alignet SET ESTADO_OPERACION = '"+ estado_operacion + "', FECHA_OPERACION = GetDate() WHERE NUMERO_PEDIDO = '"+ operationNumber+"'";  
            UpdateData(sql, CommandType.Text);
        }

        public List<base_transacciones> GetData(string sqlText, CommandType commandType = CommandType.StoredProcedure)
        {
            var dbcon = new SqlConnection(csAlignet);
            using (var dSqlServer = new DataSqlServer<base_transacciones>(new Db(dbcon)))
            {
                var dt = dSqlServer.Get(sqlText, commandType);
                return dt.ToList();
            }
        }

        public void UpdateData(string sqlText, CommandType commandType = CommandType.StoredProcedure)
        {
            var dbcon = new SqlConnection(csAlignet);
            using (var dSqlServer = new DataSqlServer<base_transacciones>(new Db(dbcon)))
            {
                dSqlServer.ExecuteNonQuery(sqlText, commandType);
            }
        }

        async static void SendMailByApi(string urlApi, StringContent xmlDoc)
        {
            using (HttpClient apiclient = new HttpClient())
            {
                using (HttpResponseMessage response = await apiclient.PostAsync(urlApi, xmlDoc))
                {
                    using (HttpContent content = response.Content) 
                    {
                        Console.WriteLine(content.ToString());
                    }
                }            
            }
        }

        private StringContent getXmlDoc(string nombre,string tarjeta,string fechacompra,string importe, string estado, string codigoautorizacion,string banco, string marca,string email)
        { 

         string data = @"<?xml version=""1.0"" encoding=""UTF-8""?><soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:cli=""http://mdwcorp.falabella.com/common/schema/clientservice"" xmlns:tns=""http://mdwcorp.falabella.com/OSB/schema/CORP/CORP/Email/Create/Req-v2014.4"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><soapenv:Header><cli:ClientService><cli:country>PE</cli:country><cli:commerce>Falabella</cli:commerce><cli:channel>Web</cli:channel></cli:ClientService></soapenv:Header><soapenv:Body><tns:emailManagementCreateRequestExp><tns:Objects xsi:type=""TriggeredSend"">" +
                       "<tns:TriggeredSendDefinition><tns:CustomerKey>%%CUSTOMERKEY%%</tns:CustomerKey></tns:TriggeredSendDefinition><tns:Subscribers>" +
                       "<tns:Attributes><tns:Name>NombreTitular</tns:Name><tns:Value>%%NOMBRE%%</tns:Value></tns:Attributes>"+
                       "<tns:Attributes><tns:Name>Tarjeta</tns:Name><tns:Value>%%TARJETA%%</tns:Value></tns:Attributes>"+
                       "<tns:Attributes><tns:Name>FechaCompra</tns:Name><tns:Value>%%FECHACOMPRA%%</tns:Value></tns:Attributes>" +
                       "<tns:Attributes><tns:Name>Importe</tns:Name><tns:Value>%%IMPORTE%%</tns:Value></tns:Attributes>" +
                       "<tns:Attributes><tns:Name>Estado</tns:Name><tns:Value>%%ESTADO%%</tns:Value></tns:Attributes>" +
                       "<tns:Attributes><tns:Name>CodigoAutorizacion</tns:Name><tns:Value>%%CODIGOAUTORIZACION%%</tns:Value></tns:Attributes>" +
                       "<tns:Attributes><tns:Name>Banco</tns:Name><tns:Value>%%BANCO%%</tns:Value></tns:Attributes>" +
                       "<tns:Attributes><tns:Name>Marca</tns:Name><tns:Value>%%MARCA%%</tns:Value></tns:Attributes>" +
                       "<tns:EmailAddress>%%EMAIL%%</tns:EmailAddress>" +
                       "<tns:SubscriberKey>%%EMAIL%%</tns:SubscriberKey></tns:Subscribers>"+
                       "<tns:Client><tns:ID>%%IDCLIENTEMAIL%%</tns:ID></tns:Client></tns:Objects></tns:emailManagementCreateRequestExp></soapenv:Body></soapenv:Envelope>";
            
            data = data.Replace("%%CUSTOMERKEY%%", CUSTOMERKEY);
            data = data.Replace("%%IDCLIENTEMAIL%%", IDCOMMERCEMAIL);
            data = data.Replace("%%NOMBRE%%", nombre);
            data = data.Replace("%%TARJETA%%", tarjeta);
            data = data.Replace("%%FECHACOMPRA%%", fechacompra);
            data = data.Replace("%%IMPORTE%%", importe);
            data = data.Replace("%%ESTADO%%", estado);
            data = data.Replace("%%CODIGOAUTORIZACION%%", codigoautorizacion);
            data = data.Replace("%%BANCO%%", banco);
            data = data.Replace("%%MARCA%%", marca);
            data = data.Replace("%%EMAIL%%", email);
            StringContent xmlDocPost = new StringContent(data, Encoding.UTF8, "application/xml");
            return xmlDocPost;
        }

        private void ConsultarAlignet(string operationNumber) 
        {
            string TEXT = IDACQUIRER + IDCOMMERCE + operationNumber + AUTHORIZATION;
            string PURCHASEVERIFICATION = SHA512(TEXT);
            string DATA = "{\"idAcquirer\":\"" + IDACQUIRER + "\",\"idCommerce\":\"" + IDCOMMERCE + "\",\"operationNumber\":\"" + operationNumber + "\",\"purchaseVerification\":\"" + PURCHASEVERIFICATION + "\"}";
            string responseData = "";
            Console.WriteLine(DATA);
            try
            {
                System.Net.WebRequest wrequest = System.Net.WebRequest.Create(APICONSULTA);
                wrequest.ContentType = "application/json";
                wrequest.Method = "POST";
                using (var streamWriter = new System.IO.StreamWriter(wrequest.GetRequestStream()))
                {
                     streamWriter.Write(DATA);
                }              
                System.Net.WebResponse wresponse = wrequest.GetResponse();
                System.IO.StreamReader responseStream = new  System.IO.StreamReader(wresponse.GetResponseStream());
                responseData = responseStream.ReadToEnd();
                Console.WriteLine(responseData);
            }
            catch (Exception ex) {
                responseData = "ERROR:" + ex.Message;
            }

        }

        private string ReverseAlignet(string operationNumber)
        {
            //string TEXT = IDACQUIRER + IDCOMMERCE + operationNumber + AUTHORIZATION;
            //string PURCHASEVERIFICATION = SHA512(TEXT);
            string DATA = ""; // "{\"idAcquirer\":\"" + IDACQUIRER + "\",\"idCommerce\":\"" + IDCOMMERCE + "\",\"operationNumber\":\"" + operationNumber + "\",\"purchaseVerification\":\"" + PURCHASEVERIFICATION + "\"}";
            string responseData = "";
            //Console.WriteLine(DATA);
            try
            {
                System.Net.WebRequest wrequest = System.Net.WebRequest.Create(APIREVERSE+"/"+operationNumber);
                wrequest.ContentType = "application/json";
                //wrequest.Headers.Add("Authorization", AUTHORIZATION);
                wrequest.Headers["Authorization"]= AUTHORIZATION;
                wrequest.Method = "DELETE"; //DELETE
                Console.WriteLine(APIREVERSE + "/" + operationNumber);
               
                using (var streamWriter = new System.IO.StreamWriter(wrequest.GetRequestStream()))
                {
                    streamWriter.Write(DATA);
                } 
                
                System.Net.WebResponse wresponse = wrequest.GetResponse();
                System.IO.StreamReader responseStream = new System.IO.StreamReader(wresponse.GetResponseStream());
                responseData = responseStream.ReadToEnd();
                //Console.WriteLine(responseData);
            }
            catch (Exception ex)
            {
                responseData = "{\"success\":\"false\",\"Exception\":\"" + ex.Message+"\"}";
            }

            return responseData;
        }

        public static string SHA512(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);
                var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                return hashedInputStringBuilder.ToString();
            }
        }

    }
}
