using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public class JsonConverter : Converter, IFastGetter<string>
    {
        protected override object ToObj(XElement element)
        {
            if (element.Elements().All(x => x.HasElements))
            {
                List<string> results = new List<string>();
                foreach (XElement elmt in element.Elements())
                {
                    results.Add(ElementToJson(elmt));
                }
                return "[" + string.Join(",", results) + "]";
            }
            else
            {
                return ElementToJson(element);
            }
        }

        protected static long BaseTicks = DateTime.Parse("1970-01-01T00:00:00Z").Ticks;

        protected string ElementToJson(XElement element)
        {
            List<string> results = new List<string>();

            List<XElement> childrenList = new List<XElement>();
            StringBuilder sb = new StringBuilder();
            foreach (XElement field in element.Elements())
            {
                XAttribute attr = field.Attribute("DataType");
                if (attr == null)
                {
                    childrenList.Add(field);
                    continue;
                }

                //
                sb.Append("\"");
                sb.Append(field.Name.LocalName);
                sb.Append("\":");
                string dataType = attr.Value;
                if (dataType == DBNullType)
                {
                    sb.Append("null,");
                }
                else if (dataType == StringType)
                {
                    sb.Append("\"");
                    sb.Append(field.Value).Replace("\r\n", "\\r\\n");
                    sb.Append("\",");
                }
                else if (dataType == ByteArrayType)
                {
                    sb.Append("\"");
                    sb.Append(field.Value);
                    sb.Append("\",");
                }
                else if (dataType == BooleanType)
                {
                    sb.Append(field.Value.ToLower());
                    sb.Append(",");
                }
                else if (dataType == DateTimeType)
                {
                    DateTime dateTime = DateTime.Parse(field.Value);
                    if (field.Attribute("TimezoneOffset") == null)
                    {
                        AppendNumericalDateTime(sb, dateTime, TimezoneOffset.Zero);
                    }
                    else
                    {
                        TimezoneOffset timezoneOffset = TimezoneOffset.Parse(field.Attribute("TimezoneOffset").Value);
                        AppendNumericalDateTime(sb, dateTime, timezoneOffset);
                    }
                }
                else if (IsNumberType(dataType))
                {
                    sb.Append(field.Value);
                    sb.Append(",");
                }
            }
            if (sb.Length > 0)
            {
                results.Add(sb.ToString().TrimEnd(','));
            }

            //
            foreach (XElement children in childrenList)
            {
                List<string> childList = new List<string>();
                foreach (XElement child in children.Elements())
                {
                    childList.Add(ElementToJson(child));
                }
                results.Add("\"" + children.Name.LocalName + "\":[" + string.Join(",", childList) + "]");
            }

            return "{" + string.Join(",", results) + "}";
        }

        public override XElement ToElement(string name, object obj, XElement schema)
        {
            if (obj is JArray)
            {
                return JArrayToElement(name, (JArray)obj, schema);
            }
            if (obj is JObject)
            {
                return JObjectToElement(name, (JToken)obj, schema);
            }
            throw new NotSupportedException(obj.GetType().ToString());
        }

        protected XElement JArrayToElement(string setName, JArray jArray, XElement schema)
        {
            XElement elements = new XElement(setName);
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;

            foreach (JToken jToken in jArray)
            {
                XElement element = JObjectToElement(elementName, jToken, schema);
                elements.Add(element);
            }
            return elements;
        }

        protected XElement JObjectToElement(string elementName, JToken obj, XElement schema)
        {
            XElement element = new XElement(elementName);

            foreach (JToken jToken in obj)
            {
                if (jToken is JProperty)
                {
                    JProperty jProperty = jToken as JProperty;
                    if (jProperty.Value is JValue)
                    {
                        XElement field = new XElement(jProperty.Name);
                        JValue jValue = jProperty.Value as JValue;

                        switch (jValue.Type)
                        {
                            case JTokenType.Array:
                                break;
                            case JTokenType.Boolean:
                                field.SetAttributeValue("DataType", BooleanType);
                                field.Value = jValue.Value.ToString();
                                break;
                            case JTokenType.Bytes:
                                field.SetAttributeValue("DataType", ByteArrayType);
                                field.Value = jValue.Value.ToString();
                                break;
                            case JTokenType.Comment:
                                break;
                            case JTokenType.Constructor:
                                break;
                            case JTokenType.Date:
                                field.SetAttributeValue("DataType", DateTimeType);
                                field.Value = ((DateTime)jValue.Value).ToCanonicalString();
                                break;
                            case JTokenType.Float:
                                field.SetAttributeValue("DataType", DoubleType);
                                field.Value = jValue.Value.ToString();
                                break;
                            case JTokenType.Guid:
                                field.SetAttributeValue("DataType", GuidType);
                                field.Value = jValue.Value.ToString();
                                break;
                            case JTokenType.Integer:
                                field.SetAttributeValue("DataType", Int32Type);
                                field.Value = jValue.Value.ToString();
                                break;
                            case JTokenType.None:
                                field.SetAttributeValue("DataType", DBNullType);
                                break;
                            case JTokenType.Null:
                                field.SetAttributeValue("DataType", DBNullType);
                                break;
                            case JTokenType.Object:
                                break;
                            case JTokenType.Property:
                                break;
                            case JTokenType.Raw:
                                break;
                            case JTokenType.String:
                                field.SetAttributeValue("DataType", StringType);
                                field.Value = jValue.Value.ToString().Replace("\\r\\n", "\r\n");
                                break;
                            case JTokenType.TimeSpan:
                                break;
                            case JTokenType.Undefined:
                                field.SetAttributeValue("DataType", DBNullType);
                                break;
                            case JTokenType.Uri:
                                break;
                            default:
                                break;
                        }

                        if (field.Attribute("DataType") != null)
                        {
                            element.Add(field);
                        }
                    }

                    if (jProperty.Value is JArray)
                    {
                        JArray jArray = jProperty.Value as JArray;

                        string setName = ((JProperty)(jToken)).Name;

                        element.Add(JArrayToElement(setName, jArray, schema));
                    }
                }
            }

            return element;
        }

        public IEnumerable<string> ToObjects(DataTable dataTable, string objectName, XElement schema)
        {
            TimezoneOffset timezoneOffset = schema.CreateTimezoneOffset();

            string setName = dataTable.TableName;
            XElement elementSchema = schema.GetElementSchemaBySetName(setName);

            List<string> list = new List<string>();
            foreach (DataRow row in dataTable.Rows)
            {
                StringBuilder sb = new StringBuilder();
                foreach (DataColumn column in dataTable.Columns)
                {
                    sb.Append("\"");
                    sb.Append(column.ColumnName);
                    sb.Append("\":");
                    object value = row[column];
                    if (value is DBNull)
                    {
                        sb.Append("null,");
                    }
                    else if (value is string)
                    {
                        sb.Append("\"");
                        sb.Append(value).Replace("\r\n", "\\r\\n");
                        sb.Append("\",");
                    }
                    else if (value is bool)
                    {
                        sb.Append(value.ToString().ToLower());
                        sb.Append(",");
                    }
                    else if (value is DateTime)
                    {
                        DateTime dateTime = (DateTime)value;
                        double dValue = (dateTime.Ticks - BaseTicks) / (double)10000;

                        XElement fieldSchema = elementSchema.Element(column.ColumnName);
                        if (fieldSchema.Element("UtcDateTime") == null)
                        {
                            AppendNumericalDateTime(sb, dateTime, timezoneOffset);
                        }
                        else
                        {
                            AppendNumericalDateTime(sb, dateTime, TimezoneOffset.Zero);
                        }
                    }
                    else if (IsNumberType(value))
                    {
                        sb.Append(value);
                        sb.Append(",");
                    }
                    else if (value is byte[])
                    {
                        byte[] bytes = (byte[])value;
                        value = "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                    }
                }
                list.Add("{" + sb.ToString().TrimEnd(',') + "}");
            }
            return list;
        }

        private void AppendNumericalDateTime(StringBuilder sb, DateTime dateTime, TimezoneOffset timezoneOffset)
        {
            double dValue = (dateTime.Ticks - BaseTicks) / (double)10000;
            sb.Append("\"");
            sb.Append(@"\/Date(");
            sb.Append(dValue);
            sb.Append(timezoneOffset.Suffix);
            sb.Append(@")\/");
            sb.Append("\",");
        }


    }
}
