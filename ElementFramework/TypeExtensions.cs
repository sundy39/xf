using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data
{
    public static class TypeExtensions
    {
        // "yyyy-MM-dd HH:mm:ss.FFFFFFF"
        public static string ToCanonicalString(this DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF");
        }

        public static void Fill(this byte[] bytes, string hexStartsWith0x)
        {
            string value = hexStartsWith0x;
            if (value.Length < 4 || value.Length % 2 == 1 || !value.StartsWith("0x")) throw new ArgumentException(value, "value");

            value = value.Substring(2);
            for (int i = 0; i < bytes.Length; i++)
            {
                int index = i * 2;
                string hex = "0x" + new string(new char[] { value[index], value[index + 1] });
                bytes[i] = Convert.ToByte(hex, 16);
            }
        }

        public static IEnumerable<XElement> ToElements(this DataTable dataTable, string elementName)
        {
            List<XElement> elements = new List<XElement>();
            foreach (DataRow row in dataTable.Rows)
            {
                XElement element = new XElement(elementName);
                foreach (DataColumn column in dataTable.Columns)
                {
                    string value;
                    object obj = row[column];
                    if (obj is DBNull)
                    {
                        value = string.Empty;
                    }
                    else if (obj is DateTime)
                    {
                        value = ((DateTime)obj).ToCanonicalString();
                    }
                    else if (obj is byte[])
                    {
                        byte[] bytes = (byte[])obj;
                        value = "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                    }
                    else
                    {
                        value = obj.ToString();
                    }
                    element.SetElementValue(column.ColumnName, value);
                }
                elements.Add(element);
            }
            return elements;
        }

        public static IEnumerable<XElement> ToElementsEx(this DataTable dataTable, string elementName, XElement schema)
        {
            string timezoneOffset = schema.Attribute(Glossary.TimezoneOffset).Value;
            XElement elementSchema = schema.GetElementSchema(elementName);

            List<XElement> elements = new List<XElement>();
            foreach (DataRow row in dataTable.Rows)
            {
                XElement element = new XElement(elementName);
                foreach (DataColumn column in dataTable.Columns)
                {
                    string value;
                    object obj = row[column];
                    if (obj is DBNull)
                    {
                        value = string.Empty;
                    }
                    else if (obj is DateTime)
                    {
                        value = ((DateTime)obj).ToCanonicalString();
                    }
                    else if (obj is byte[])
                    {
                        byte[] bytes = (byte[])obj;
                        value = "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                    }
                    else
                    {
                        value = obj.ToString();
                    }
                    XElement field = new XElement(column.ColumnName, value);
                    field.SetAttributeValue("DataType", obj.GetType().ToString());
                    if (obj.GetType() == typeof(DateTime))
                    {
                        XElement fieldSchema = elementSchema.Element(column.ColumnName);
                        if (fieldSchema.Element("UtcDateTime") == null)
                        {
                            field.SetAttributeValue("TimezoneOffset", timezoneOffset);
                        }
                    }
                    element.Add(field);
                }
                elements.Add(element);
            }
            return elements;
        }

        public static bool IsNumberType(Type type)
        {
            return (type == typeof(SByte) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64)
                || type == typeof(Byte) || type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64)
                || type == typeof(Decimal) || type == typeof(Single) || type == typeof(Double));
        }


    }
}
