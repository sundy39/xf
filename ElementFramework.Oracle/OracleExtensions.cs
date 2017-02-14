using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Objects.Oracle
{
    internal static class OracleExtensions
    {
        public static string DecorateDateTimeValue(this DateTime value, string sqlDbType)
        {
            if (sqlDbType == "date")
            {
                string sValue = value.ToString("yyyy-MM-dd HH:mm:ss");
                return string.Format("TO_DATE('{0}', 'YYYY-MM-DD HH24:MI:SS')", sValue);
            }

            if (sqlDbType.StartsWith("timestamp"))
            {
                string sValue = value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF");
                return string.Format("TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS.FF9')", sValue);
            }

            throw new NotSupportedException(sqlDbType);
        }


    }
}
