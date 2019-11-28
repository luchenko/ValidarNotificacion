using Release.Helper.Data.Core;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;

namespace BO.Integracion.Siebel
{
    public class DataSqlServer<T> : BaseUnitOfWorkAdo where T : class, new()
    {
        public DataSqlServer(IDb db) : base(db)
        {

        }

        public IList<T> Get(string sqlText, CommandType commandType = CommandType.StoredProcedure)
        {
            var param = new Parameter[] { };
            var items = this.ExecuteReader<T>(sqlText, commandType, ref param);
            return items.ToList();
        }

        public DataTable GetData(string sqlText, CommandType commandType = CommandType.StoredProcedure)
        {
            var tb = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlText, (SqlConnection)Connection))
            {
                cmd.CommandType = commandType;
                cmd.Parameters.Clear();

                using (var dr = cmd.ExecuteReader())
                {
                    tb.Load(dr);
                }
            }
            return tb;
        }

      public void ExecuteNonQuery(string sqlText, CommandType commandType = CommandType.StoredProcedure)
        {
            using (SqlCommand cmd = new SqlCommand(sqlText, (SqlConnection)Connection))
            {
                cmd.CommandType = commandType;
                cmd.Parameters.Clear();

                cmd.ExecuteNonQuery();
            }
        }

   

    }
}