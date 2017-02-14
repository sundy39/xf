using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public class DictionaryConverter<T> : Converter, IFastGetter<T>
        where T : IDictionary<String, Object>, new()
    {
        protected override object ToObj(XElement element)
        {
            if (element.Elements().All(x => x.HasElements))
            {
                List<T> results = new List<T>();
                foreach (XElement elmt in element.Elements())
                {
                    results.Add(ElementToObject(elmt));
                }
                return results;
            }
            else
            {
                return ElementToObject(element);
            }
        }

        protected virtual T ElementToObject(XElement element)
        {
            T obj = new T();
            IDictionary<String, Object> dict = (IDictionary<String, Object>)obj;

            List<XElement> childrenList = new List<XElement>();

            FillValues(dict, childrenList, element);

            //
            foreach (XElement children in childrenList)
            {
                List<T> childList = new List<T>();
                foreach (XElement child in children.Elements())
                {
                    childList.Add(ElementToObject(child));
                }
                dict.Add(children.Name.LocalName, childList);
            }

            return obj;
        }

        protected void FillValues(IDictionary<String, Object> dict, List<XElement> childrenList, XElement element)
        {
            foreach (XElement field in element.Elements())
            {
                XAttribute attr = field.Attribute("DataType");
                if (attr == null)
                {
                    childrenList.Add(field);
                    continue;
                }
                //
                string dataType = attr.Value;
                if (dataType == DBNullType)
                {
                    dict.Add(field.Name.LocalName, null);
                }
                else if (dataType == StringType)
                {
                    dict.Add(field.Name.LocalName, field.Value);
                }
                else if (IsNumberType(dataType))
                {
                    object value = Convert.ChangeType(field.Value, Type.GetType(dataType));
                    dict.Add(field.Name.LocalName, value);
                }
                else if (dataType == BooleanType)
                {
                    dict.Add(field.Name.LocalName, bool.Parse(field.Value));
                }
                else if (dataType == DateTimeType)
                {
                    dict.Add(field.Name.LocalName, DateTime.Parse(field.Value));
                }
                else if (dataType == GuidType)
                {
                    Guid guid = Guid.Parse(field.Value);
                    dict.Add(field.Name.LocalName, guid);
                }
                else if (dataType == ByteArrayType)
                {
                    byte[] bytes = GetBytes(field.Value);
                    dict.Add(field.Name.LocalName, bytes);
                }
            }
        }

        protected byte[] GetBytes(string value)
        {
            byte[] bytes = new byte[value.Length / 2 - 1];
            bytes.Fill(value);

            return bytes;
        }

        public override XElement ToElement(string name, object obj, XElement schema)
        {
            if (obj is IEnumerable<T>)
            {
                return ObjectsToElement(name, (IEnumerable<T>)obj, schema);
            }
            if (obj is T)
            {
                return ObjectToElement(name, (T)obj, schema);
            }
            throw new NotSupportedException(obj.GetType().ToString());
        }

        protected XElement ObjectsToElement(string setName, IEnumerable<T> objs, XElement schema)
        {
            XElement elements = new XElement(setName);
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;

            foreach (var obj in objs)
            {
                XElement element = ObjectToElement(elementName, obj, schema);
                elements.Add(element);
            }
            return elements;
        }

        protected XElement ObjectToElement(string elementName, T obj, XElement schema)
        {
            XElement element = new XElement(elementName);
            IDictionary<String, Object> dict = (IDictionary<String, Object>)obj;
            foreach (KeyValuePair<String, Object> pair in dict)
            {
                if (pair.Value is IEnumerable<T>)
                {
                    element.Add(ObjectsToElement(pair.Key, pair.Value as IEnumerable<T>, schema));
                }
                else
                {
                    XElement field = new XElement(pair.Key);
                    if (pair.Value == null)
                    {
                        field.SetAttributeValue("DataType", DBNullType);
                    }
                    else
                    {
                        if (pair.Value is DateTime)
                        {
                            field.Value = ((DateTime)pair.Value).ToCanonicalString();
                            field.SetAttributeValue("DataType", DateTimeType);
                        }
                        else if (pair.Value is byte[])
                        {
                            field.Value = "0x" + BitConverter.ToString((byte[])pair.Value).Replace("-", string.Empty);
                            field.SetAttributeValue("DataType", ByteArrayType);
                        }
                        else
                        {
                            field.Value = pair.Value.ToString();
                            field.SetAttributeValue("DataType", pair.Value.GetType().ToString());
                        }
                    }
                    element.Add(field);
                }
            }
            return element;
        }

        public IEnumerable<T> ToObjects(DataTable dataTable, string objectName, XElement schema)
        {
            List<T> list = new List<T>();
            foreach (DataRow row in dataTable.Rows)
            {
                T obj = new T();
                IDictionary<String, Object> dict = obj as IDictionary<String, Object>;
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (row[column] is DBNull)
                    {
                        dict.Add(column.ColumnName, null);
                    }
                    else
                    {
                        dict.Add(column.ColumnName, row[column]);
                    }
                }
                list.Add(obj);
            }
            return list;
        }
    }
}
