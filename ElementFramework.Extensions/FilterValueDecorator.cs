using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Element
{
    public static class FilterValueDecorator
    {

        public static bool IsNumberType(this Type type)
        {
            return (type == typeof(SByte) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64)
                || type == typeof(Byte) || type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64)
                || type == typeof(Decimal) || type == typeof(Single) || type == typeof(Double));
        }

        public static string Decorate(string value, Type dataType)
        {            
            if (dataType == typeof(String) || dataType == typeof(Char))
            {
                return "'" + value + "'";
            }

            if (string.IsNullOrEmpty(value)) return "null";
            if (IsNumberType(dataType))
            {
                return value;
            }
            if (dataType == typeof(DateTime))
            {
                return "datetime'" + DateTime.Parse(value).ToCanonicalString() + "'";
            }
            if (dataType == typeof(bool))
            {
                if (value == "True" || value == "true") return "true";
                if (value == "False" || value == "false") return "false";
                throw new FormatException(string.Format("value is not equal to the value of the {0} or {1}", "false", "true"));
            }
            if (dataType == typeof(Guid))
            {
                return "'" + value + "'";
            }
            if (dataType == typeof(Byte[]))
            {
                return value;
            }
            if (dataType==typeof(TimeSpan))
            {
                string result = TimeSpan.Parse(value).ToString();
                return "'" + result + "'";
            }

            throw new NotSupportedException(dataType.ToString());
        }


    }
}
