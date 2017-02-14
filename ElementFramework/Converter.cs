using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Element
{
    public abstract class Converter
    {
        protected static string StringType = typeof(string).ToString();
        protected static string BooleanType = typeof(bool).ToString();
        protected static string DateTimeType = typeof(DateTime).ToString();
        protected static string TimeSpanType = typeof(TimeSpan).ToString();
        protected static string ByteArrayType = typeof(byte[]).ToString();
        protected static string GuidType = typeof(Guid).ToString();
        protected static string DBNullType = typeof(DBNull).ToString();

        protected static string SByteType = typeof(SByte).ToString();
        protected static string Int16Type = typeof(Int16).ToString();
        protected static string Int32Type = typeof(Int32).ToString();
        protected static string Int64Type = typeof(Int64).ToString();
        protected static string ByteType = typeof(Byte).ToString();
        protected static string UInt16Type = typeof(UInt16).ToString();
        protected static string UInt32Type = typeof(UInt32).ToString();
        protected static string UInt64Type = typeof(UInt64).ToString();
        protected static string DecimalType = typeof(Decimal).ToString();
        protected static string SingleType = typeof(Single).ToString();
        protected static string DoubleType = typeof(Double).ToString();

        protected bool IsNumberType(string type)
        {
            return type == SByteType || type == Int16Type || type == Int32Type || type == Int64Type
                || type == ByteType || type == UInt16Type || type == UInt32Type || type == UInt64Type
                || type == DecimalType || type == SingleType || type == DoubleType;
        }

        protected bool IsNumberType(object obj)
        {
            return obj is SByte || obj is Int16 || obj is Int32 || obj is Int64
                || obj is Byte || obj is UInt16 || obj is UInt32 || obj is UInt64
                || obj is Decimal || obj is Single || obj is Double;
        }

        protected void PatchValidationResults(XElement element)
        {
            if (element.Name.LocalName == "ValidationResults")
            {
                XElement error = element.Descendants("ValidationError").FirstOrDefault();
                if (error.Element("XPath") != null && error.Element("ErrorMessage") != null)
                {
                    if (error.Element("XPath").Attribute("DataType") == null && error.Element("ErrorMessage").Attribute("DataType") == null)
                    {
                        // Is a ValidationResults
                        IEnumerable<XElement> nodes = element.Descendants().Where(x => x.Name.LocalName == "XPath" || x.Name.LocalName == "ErrorMessage");
                        foreach (XElement node in nodes)
                        {
                            node.SetAttributeValue("DataType", StringType);
                        }
                    }
                }
            }
        }

        public virtual object ToObject(XElement element)
        {
            PatchValidationResults(element);
            return ToObj(element);
        }

        protected abstract object ToObj(XElement element);

        public abstract XElement ToElement(string name, object obj, XElement schema);


    }
}
