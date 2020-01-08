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
using Newtonsoft.Json;
using System.Configuration;
using RestSharp;

namespace Cliente360.Integracion.Alignet
{
    public class IntegracionAlignetManager
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static string csAlignet;

        private readonly string APICONSULTA_ALIGNET = ConfigurationManager.AppSettings["APICONSULTA_ALIGNET"];
        private readonly string APINOTIFICACIONES_URL = ConfigurationManager.AppSettings["APINOTIFICACIONES_URL"];
        private readonly string APINOTIFICACIONES_KEY = ConfigurationManager.AppSettings["APINOTIFICACIONES_KEY"];
        private readonly string APINOTIFICACIONES_REGISTER = "/api/register";

        private readonly string IDACQUIRER = ConfigurationManager.AppSettings["IDACQUIRER"];
        private readonly string IDCOMMERCE = ConfigurationManager.AppSettings["IDCOMMERCE"];
        //private string OPERATIONNUMBER = "62170007";
        private readonly string AUTHORIZATION_CONSULTA = ConfigurationManager.AppSettings["AUTHORIZATION_CONSULTA"];


        private static readonly string CUSTOMERKEY01 = ConfigurationManager.AppSettings["CUSTOMERKEY01"];
        private static readonly string CUSTOMERKEY02 = ConfigurationManager.AppSettings["CUSTOMERKEY02"];
        private static readonly string CUSTOMERKEY03 = ConfigurationManager.AppSettings["CUSTOMERKEY03"];
        private static readonly string IDCOMMERCEMAIL = ConfigurationManager.AppSettings["IDCOMMERCEMAIL"];
        //System.Configuration.ConfigurationManager

        private static readonly string DIAS_PROCESO_EXTORNO = ConfigurationManager.AppSettings["DIAS_PROCESO_EXTORNO"]; //4

        //private static readonly string cadenaConexion = ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
        private static readonly string cadenaConexion = ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;

       // private static readonly string COD_RESULT_LIQUIDADO = ConfigurationManager.AppSettings["COD_RESULT_LIQUIDADO"];
        private static readonly string COD_RESULT_EXTORNADO = ConfigurationManager.AppSettings["COD_RESULT_EXTORNADO"];

        private static readonly string CODEST_OKVALIDACION = ConfigurationManager.AppSettings["CODEST_OKVALIDACION"]; //
        private static readonly string CODEST_OKEXTORNADO = ConfigurationManager.AppSettings["CODEST_OKEXTORNADO"];
        private static readonly string CODERROR_VALIDACION = ConfigurationManager.AppSettings["CODERROR_VALIDACION"];
        private static readonly string CODERROR_NOTIFICACION = ConfigurationManager.AppSettings["CODERROR_NOTIFICACION"];
        private static readonly string ESTADO_EXTORNADO = ConfigurationManager.AppSettings["ESTADO_EXTORNADO"];
        //private static readonly string VALIDAR_EXTORNO_REALIZADO = ConfigurationManager.AppSettings["VALIDAR_EXTORNO_REALIZADO"];  

        //result

        public IntegracionAlignetManager(string csalignet)
        {
            csAlignet = csalignet;
        }
        public void Start()
        {

            //Newtonsoft.Json.Linq.JObject objreversa;
            string[] estados = { CODEST_OKEXTORNADO, CODERROR_VALIDACION };
            Newtonsoft.Json.Linq.JObject objNotificacion;

            try
            {
                _logger.Info("Inicio del procesamiento #3 Validar-Notificacion");
                var transacciones_alignet = ObtenerTransaccionesParaValidar(estados);

                foreach (base_transacciones el in transacciones_alignet)
                {

                    string _estado = GET_RESULT_CONSULTA_ALIGNET(el.NUMERO_PEDIDO);

                    if (_estado == COD_RESULT_EXTORNADO)
                    {
                        ActualizarTransaccionesAlignet(el.ID, CODEST_OKVALIDACION, ESTADO_EXTORNADO);
                        var notificacionBase = new object {};

                        switch (el.TIPO_TRANSACCION)
                        {
                            case 1:
                                notificacionBase = getNotificacionBase(el.ID.ToString(), el.NUMERO_PEDIDO, el.NOMBRE_TITULAR, el.NUMERO_TARJETA, string.Format("{0:dd-MM-yyyy}", el.FECHA_PEDIDO), el.IMPORTE_PEDIDO, ESTADO_EXTORNADO, el.CODIGO_AUTORIZACION, el.BANCO, el.MEDIO_PAGO, el.EMAIL,CUSTOMERKEY01);
                                break;
                            case 2:
                                notificacionBase = getNotificacionBase(el.ID.ToString(), el.NUMERO_PEDIDO, el.NOMBRE_TITULAR, el.NUMERO_TARJETA, string.Format("{0:dd-MM-yyyy}", el.FECHA_PEDIDO), el.IMPORTE_PEDIDO, ESTADO_EXTORNADO, el.CODIGO_AUTORIZACION, el.BANCO, el.MEDIO_PAGO, el.EMAIL,CUSTOMERKEY02);
                                break;
                            default:
                                notificacionBase = getNotificacionBase(el.ID.ToString(), el.NUMERO_PEDIDO, el.NOMBRE_TITULAR, el.NUMERO_TARJETA, string.Format("{0:dd-MM-yyyy}", el.FECHA_PEDIDO), el.IMPORTE_PEDIDO, ESTADO_EXTORNADO, el.CODIGO_AUTORIZACION, el.BANCO, el.MEDIO_PAGO, el.EMAIL,CUSTOMERKEY03);
                                break;
                        }

                        //notification
                        string result_notificacion = NOTIFICACION_REGISTRAR(notificacionBase);
                        objNotificacion = Newtonsoft.Json.Linq.JObject.Parse(result_notificacion);
                        objNotificacion.TryGetValue("success", out Newtonsoft.Json.Linq.JToken result);
                       
                        if (result.ToString().Trim() != "True")
                        {
                            ActualizarTransaccionesAlignet(el.ID, CODERROR_NOTIFICACION, el.ESTADO_TRANSACCION);
                            //Console.WriteLine("Error Notificacion : " + el.NUMERO_PEDIDO);
                        }  else Console.WriteLine("OK Notificacion : " + el.NUMERO_PEDIDO);

                    }
                    else
                    {
                        ActualizarTransaccionesAlignet(el.ID, CODERROR_VALIDACION, el.ESTADO_TRANSACCION);
                       // Console.WriteLine("Error Notificacion : " + el.NUMERO_PEDIDO);
                    }
                }

                _logger.Info("Fin del procesamiento Validacion-Notificacion");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private List<base_transacciones> ObtenerTransaccionesParaValidar(string[] estado)
        {
            _logger.Info("Inicio de la carga de transacciones alignet...");
            string sql = "Select * from base_transacciones_alignet where ESTADO_OPERACION in ('" + String.Join("','", estado) + "') AND  convert(datetime, dateadd(d, -" + DIAS_PROCESO_EXTORNO + ", GETDATE()), 103) <= convert(datetime, FECHA_PEDIDO, 103) ";
            var transacciones = GetData(sql, CommandType.Text);
            _logger.Info(string.Format("Se obtuvieron {0} transacciones...", transacciones.Count));
            return transacciones;
        }

        public void ActualizarTransaccionesAlignet(long id, string estado_operacion, string estado_transaccion)
        {
            //_logger.Info("Actualiza carga de transacciones alignet...");
            string sql = "UPDATE base_transacciones_alignet SET ESTADO_OPERACION = '" + estado_operacion + "',ESTADO_TRANSACCION = '" + estado_transaccion + "', FECHA_OPERACION = GetDate() WHERE ID = '" + id.ToString() + "'";
            UpdateData(sql, CommandType.Text);
        }

        public List<base_transacciones> GetData(string sqlText, CommandType commandType = CommandType.StoredProcedure)
        {
            var dbcon = new SqlConnection(csAlignet);
            using (var dSqlServer = new BO.Integracion.Siebel.DataSqlServer<base_transacciones>(new Db(dbcon)))
            {
                var dt = dSqlServer.Get(sqlText, commandType);
                return dt.ToList();
            }
        }

        public void UpdateData(string sqlText, CommandType commandType = CommandType.StoredProcedure)
        {
            var dbcon = new SqlConnection(csAlignet);
            using (var dSqlServer = new BO.Integracion.Siebel.DataSqlServer<base_transacciones>(new Db(dbcon)))
            {
                dSqlServer.ExecuteNonQuery(sqlText, commandType);
            }
        }

        private object getNotificacionBase(string identificador1, string identificador2, string nombre, string tarjeta, string fechacompra, string importe, string estado, string codigoautorizacion, string banco, string marca, string email,string CUSTOMERKEY)
        {
            var variables = new Dictionary<string, string>();
            variables.Add("NombreTitular", nombre);
            variables.Add("Tarjeta", tarjeta);
            variables.Add("FechaCompra", fechacompra);
            variables.Add("Importe", importe);
            variables.Add("Estado", estado);
            variables.Add("CodigoAutorizacion", codigoautorizacion);
            variables.Add("Banco", banco);
            variables.Add("Marca", marca);
            variables.Add("EMAIL", email);

            var notificationBase = new {
                id_notification_type = 1,
                id_business_type = 2,
                identifier_1 =  identificador1,
                identifier_2 = identificador2,
                data = variables,
                contact_input = email,
                contact_status = "valid",
                template = CUSTOMERKEY,
                status = 0
            };

            return notificationBase;
        }

        private StringContent getXmlDoc(string nombre, string tarjeta, string fechacompra, string importe, string estado, string codigoautorizacion, string banco, string marca, string email)
        {

            string data = @"<?xml version=""1.0"" encoding=""UTF-8""?><soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:cli=""http://mdwcorp.falabella.com/common/schema/clientservice"" xmlns:tns=""http://mdwcorp.falabella.com/OSB/schema/CORP/CORP/Email/Create/Req-v2014.4"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><soapenv:Header><cli:ClientService><cli:country>PE</cli:country><cli:commerce>Falabella</cli:commerce><cli:channel>Web</cli:channel></cli:ClientService></soapenv:Header><soapenv:Body><tns:emailManagementCreateRequestExp><tns:Objects xsi:type=""TriggeredSend"">" +
                          "<tns:TriggeredSendDefinition><tns:CustomerKey>%%CUSTOMERKEY%%</tns:CustomerKey></tns:TriggeredSendDefinition><tns:Subscribers>" +
                          "<tns:Attributes><tns:Name>NombreTitular</tns:Name><tns:Value>%%NOMBRE%%</tns:Value></tns:Attributes>" +
                          "<tns:Attributes><tns:Name>Tarjeta</tns:Name><tns:Value>%%TARJETA%%</tns:Value></tns:Attributes>" +
                          "<tns:Attributes><tns:Name>FechaCompra</tns:Name><tns:Value>%%FECHACOMPRA%%</tns:Value></tns:Attributes>" +
                          "<tns:Attributes><tns:Name>Importe</tns:Name><tns:Value>%%IMPORTE%%</tns:Value></tns:Attributes>" +
                          "<tns:Attributes><tns:Name>Estado</tns:Name><tns:Value>%%ESTADO%%</tns:Value></tns:Attributes>" +
                          "<tns:Attributes><tns:Name>CodigoAutorizacion</tns:Name><tns:Value>%%CODIGOAUTORIZACION%%</tns:Value></tns:Attributes>" +
                          "<tns:Attributes><tns:Name>Banco</tns:Name><tns:Value>%%BANCO%%</tns:Value></tns:Attributes>" +
                          "<tns:Attributes><tns:Name>Marca</tns:Name><tns:Value>%%MARCA%%</tns:Value></tns:Attributes>" +
                          "<tns:EmailAddress>%%EMAIL%%</tns:EmailAddress>" +
                          "<tns:SubscriberKey>%%EMAIL%%</tns:SubscriberKey></tns:Subscribers>" +
                          "<tns:Client><tns:ID>%%IDCLIENTEMAIL%%</tns:ID></tns:Client></tns:Objects></tns:emailManagementCreateRequestExp></soapenv:Body></soapenv:Envelope>";

            data = data.Replace("%%CUSTOMERKEY%%", CUSTOMERKEY01);
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

        private string NOTIFICACION_REGISTRAR(object DATA)
        {
            string responseData = "";
            try
            {
                string URL_API = APINOTIFICACIONES_URL + APINOTIFICACIONES_REGISTER + "?code=" + APINOTIFICACIONES_KEY;
                var client = new RestClient(URL_API);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.Parameters.Clear();
                string body = Newtonsoft.Json.JsonConvert.SerializeObject(DATA);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                responseData = response.Content;
            }
            catch (Exception ex)
            {
                responseData = "{\"success\":\"false\",\"Exception\":\"" + ex.Message + "\"}";
            }
            
            return responseData;
        }
        private string GET_RESULT_CONSULTA_ALIGNET(string operationNumber) 
        {
            string TEXT = IDACQUIRER + IDCOMMERCE + operationNumber + AUTHORIZATION_CONSULTA;
            string PURCHASEVERIFICATION = SHA512(TEXT);
            string DATA = "{\"idAcquirer\":\"" + IDACQUIRER + "\",\"idCommerce\":\"" + IDCOMMERCE + "\",\"operationNumber\":\"" + operationNumber + "\",\"purchaseVerification\":\"" + PURCHASEVERIFICATION + "\"}";
            string responseData = "";
            Newtonsoft.Json.Linq.JObject objreversa;
            //Console.WriteLine(DATA);
            try
            {
                System.Net.WebRequest wrequest = System.Net.WebRequest.Create(APICONSULTA_ALIGNET);
                wrequest.ContentType = "application/json";
                wrequest.Method = "POST";
                using (var streamWriter = new System.IO.StreamWriter(wrequest.GetRequestStream()))
                {
                     streamWriter.Write(DATA);
                }              
                System.Net.WebResponse wresponse = wrequest.GetResponse();
                System.IO.StreamReader responseStream = new  System.IO.StreamReader(wresponse.GetResponseStream());
                responseData = responseStream.ReadToEnd();
                objreversa = Newtonsoft.Json.Linq.JObject.Parse(responseData);
                objreversa.TryGetValue("result", out Newtonsoft.Json.Linq.JToken result);
                responseData = result.ToString().Trim();
            }
            catch (Exception ex) {
                responseData = "ERROR:" + ex.Message;
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
                return hashedInputStringBuilder.ToString().ToLower();
            }
        }

    }
}
