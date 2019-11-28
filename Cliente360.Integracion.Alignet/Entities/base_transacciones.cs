using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cliente360.Integracion.Alignet.Entities
{
    public class base_transacciones
    {
        public Int64 ID { get; set; }
        public string NUMERO_PEDIDO { get; set; }
        public string ESTADO_TRANSACCION { get; set; }
        public string COMERCIO { get; set; }
        public string PAN { get; set; }
        public string MONTO { get; set; }
        public string FECHA_PEDIDO { get; set; }
        public string VCI { get; set; }
        public string TITULAR { get; set; }
        public string EMAIL { get; set; }
        public string CODIGO_AUTORIZACION { get; set; }
        public string MARCA_TARJETA { get; set; }
        public string TIPO_TARJETA { get; set; }
        public string BANCO { get; set; }
        public string CODIGO_UNICO { get; set; }
        public string ORDEN_COMPRA { get; set; }
        public string ASL { get; set; }
        public string CANAL { get; set; }
        public Int32 ESTADO_OPERACION { get; set; }
        public DateTime FECHA_OPERACION { get; set; }
    }
}
