using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cliente360.Integracion.Alignet.Entities
{
    public class base_transacciones
    {
        public long ID { get; set; }
        public string CODIGO_UNICO { get; set; }
        public string NUMERO_ORDEN { get; set; }
        public string NUMERO_PEDIDO { get; set; }
        public string ESTADO_TRANSACCION { get; set; }
        public string CODIGO_COMERCIO { get; set; }
        public string NUMERO_TARJETA { get; set; }
        public string IMPORTE_PEDIDO { get; set; }
        public DateTime FECHA_PEDIDO { get; set; }
        public string NOMBRE_TITULAR { get; set; }
        public string APELLIDO_TITULAR { get; set; }
        public string EMAIL { get; set; }
        public string CODIGO_AUTORIZACION { get; set; }
        public string MEDIO_PAGO { get; set; }
        public string BANCO { get; set; }
        public int ESTADO_OPERACION { get; set; }
        public DateTime FECHA_OPERACION { get; set; }
    }
}
