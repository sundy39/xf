using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data;
using XData.Data.Element;
using XData.Data.Extensions;
using XData.Data.Schema;

namespace XData.Data.Components
{
    public class DataSource
    {
        protected ElementContext ElementContext;
        protected SpecifiedConfigGetter SpecifiedConfigGetter;

        public DataSource(ElementContext elementContext, SpecifiedConfigGetter specifiedConfigGetter)
        {
            ElementContext = elementContext;
            SpecifiedConfigGetter = specifiedConfigGetter;
        }

        // ... // .../Index
        // .../Create
        // .../Edit/{id}
        // .../Delete/{id}
        public virtual XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            string referrer = nameValuePairs.GetValue("referrer");
            string[] rArray = referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int length = rArray.Length;
            string last = rArray.Last();

            string action = "Index";
            string setName = last;
            string id = null;
            if (last == "Index" || last == "Create")
            {
                action = last;
                setName = rArray[length - 2];
            }
            else if (rArray.Length > 1)
            {
                string last_1 = rArray[length - 2];
                if (last_1 == "Edit" || last_1 == "Delete" || last_1 == "Details")
                {
                    action = last_1;
                    setName = rArray[length - 3];
                    id = last;
                }
            }

            XElement result;
            XElement schema = GetSchema(nameValuePairs);
            XElement elementSchema = schema.GetElementSchemaBySetName(setName);
            string elementName = elementSchema.Name.LocalName;
            if (action == "Index")
            {
                result = GetSet(elementName, null, null, null, schema, accept);
            }
            else if (action == "Create")
            {
                result = GetDefault(elementName, null, schema, accept);
            }
            else // "Edit" || "Delete" || "Details"
            {
                XElement keySchema = elementSchema.GetKeySchema();
                if (keySchema.Elements().Count() > 1) throw new SchemaException("Key mismatch", schema);

                XElement keyFieldSchema = keySchema.Elements().First();
                Type dataType = Type.GetType(keyFieldSchema.Attribute("DataType").Value);
                string format = dataType.IsNumberType() ? "{0} eq {1}" : "{0} eq '{1}'";
                string filter = string.Format(format, keyFieldSchema.Name.LocalName, id);

                result = GetSet(elementName, null, filter, null, schema, accept);
            }

            return result;
        }

        public virtual XElement GetCount(IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            string referrer = nameValuePairs.GetValue("referrer");
            string[] rArray = referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int length = rArray.Length;
            string last = rArray.Last();

            string action = "Index";
            string setName = last;
            if (last == "Index" || last == "Create")
            {
                action = last;
                setName = rArray[length - 2];
            }
            else if (rArray.Length > 1)
            {
                string last_1 = rArray[length - 2];
                if (last_1 == "Edit" || last_1 == "Delete" || last_1 == "Details")
                {
                    action = last_1;
                    setName = rArray[length - 3];
                }
            }

            if (action != "Index") throw new NotSupportedException(action);

            XElement schema = GetSchema(nameValuePairs);
            XElement elementSchema = schema.GetElementSchemaBySetName(setName);
            string elementName = elementSchema.Name.LocalName;

            return GetCount(elementName, null, schema, accept);
        }

        protected XElement GetDefault(string elementName, string select, XElement schema, string accept)
        {
            ElementQuerier elementQuerier = GetElementQuerier(accept);
            XElement result = elementQuerier.GetDefault(elementName, select, schema);
            result.SetAttributeValue("Accept", accept);
            return result;
        }

        protected XElement GetSet(string elementName, string select, string filter, string orderby, XElement schema, string accept)
        {
            XElement result;
            if (accept == "json")
            {
                XElement elementSchema = schema.GetElementSchema(elementName);
                string setName = elementSchema.Attribute("Set").Value;
                result = new XElement(setName);
                IEnumerable<string> results = new JsonQuerier(ElementContext).GetSet(elementName, select, filter, orderby, schema);
                string content = string.Join(",", results);
                content = string.Format("[{0}]", content);
                result.SetAttributeValue("Content", content);
            }
            else
            {
                ElementQuerier elementQuerier = GetElementQuerier(accept);
                result = elementQuerier.GetSet(elementName, select, filter, orderby, schema);
            }

            result.SetAttributeValue("Accept", accept);
            return result;
        }

        protected XElement GetPage(string elementName, string select, string filter, string orderby, int skip, int take, XElement schema, string accept)
        {
            XElement result;
            if (accept == "json")
            {
                XElement elementSchema = schema.GetElementSchema(elementName);
                string setName = elementSchema.Attribute("Set").Value;
                result = new XElement(setName);
                IEnumerable<string> results = new JsonQuerier(ElementContext).GetPage(elementName, select, filter, orderby, skip, take, schema);
                string content = string.Join(",", results);
                content = string.Format("[{0}]", content);
                result.SetAttributeValue("Content", content);
            }
            else
            {
                ElementQuerier elementQuerier = GetElementQuerier(accept);
                result = elementQuerier.GetPage(elementName, select, filter, orderby, skip, take, schema);
            }

            result.SetAttributeValue("Accept", accept);
            return result;
        }

        protected XElement GetCount(string elementName, string filter, XElement schema, string accept)
        {
            int count = new ElementQuerier(ElementContext).GetCount(elementName, filter, schema);

            XElement result = new XElement("Count", count);
            if (accept == "json")
            {
                result.SetAttributeValue("DataType", typeof(int).ToString());
                result = new XElement("Element", result);
            }

            result.SetAttributeValue("Accept", accept);
            return result;
        }

        // .../Create
        public virtual XElement Create(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            XElement element;
            XElement schema = GetSchema(nameValuePairs);
            XElement master = null;
            if (value is XElement)
            {
                element = value as XElement;

                master = element.Elements().FirstOrDefault(x => x.HasElements);
            }
            else
            {
                string referrer = nameValuePairs.GetValue("referrer");
                string[] rArray = referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                int length = rArray.Length;
                string setName = rArray[length - 2];

                element = ToElement(setName, value, schema);
                AttachAttributes(element, nameValuePairs);

                master = GetMaster(value, nameValuePairs, schema);
            }

            AlignToSelect(element, nameValuePairs);

            if (master != null)
            {
                schema.SetRelatedValue(element, master);
            }
            ElementContext.Create(element, schema);
            return element;
        }

        protected virtual XElement GetMaster(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs, XElement schema)
        {
            JProperty master = (JProperty)((JContainer)value).Last;
            if (master.Value is JObject)
            {
                XElement elementSchema = schema.GetElementSchema(master.Name);
                string setName = elementSchema.Attribute("Set").Value;
                XElement element = ToElement(setName, master.Value, schema);
                return element;
            }
            return null;
        }

        // .../Edit/{id}
        public virtual XElement Update(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            XElement element;
            XElement schema = GetSchema(nameValuePairs);
            if (value is XElement)
            {
                element = value as XElement;
            }
            else
            {
                string referrer = nameValuePairs.GetValue("referrer");
                string[] rArray = referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                int length = rArray.Length;
                string setName = rArray[length - 3];
                string id = rArray.Last();

                element = ToElement(setName, value, schema);
                AttachAttributes(element, nameValuePairs);

                XElement keySchema = schema.GetElementSchemaBySetName(setName).GetKeySchema();
                if (keySchema.Elements().Count() == 1)
                {
                    string keyFieldName = keySchema.Elements().First().Name.LocalName;
                    if (element.Element(keyFieldName) == null)
                    {
                        element.SetElementValue(keyFieldName, id);
                    }
                }
            }

            AlignToSelect(element, nameValuePairs);
            ElementContext.Update(element, schema);
            return element;
        }

        protected virtual void AlignToSelect(XElement element, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
        }

        // .../Delete/{id}
        public virtual XElement Delete(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            string referrer = nameValuePairs.GetValue("referrer");
            string[] rArray = referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int length = rArray.Length;
            string setName = rArray[length - 3];
            string id = rArray.Last();

            XElement schema = GetSchema(nameValuePairs);
            XElement elementSchema = schema.GetElementSchemaBySetName(setName);
            XElement keySchema = elementSchema.GetKeySchema();
            if (keySchema.Elements().Count() != 1) throw new SchemaException("Key mismatch", schema);

            string keyFieldName = keySchema.Elements().First().Name.LocalName;
            XElement element = new XElement(elementSchema.Name.LocalName);
            element.SetElementValue(keyFieldName, id);
            AttachAttributes(element, nameValuePairs);

            ElementContext.Delete(element, schema);
            return element;
        }

        public virtual XElement Delete(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            XElement element;
            XElement schema = GetSchema(nameValuePairs);
            if (value is XElement)
            {
                element = value as XElement;
                AlignToSelect(element, nameValuePairs);
                ElementContext.Delete(element, schema);
                return element;
            }

            throw new NotSupportedException(value.GetType().FullName);
        }

        protected XElement ToElement(string setName, object value, XElement schema)
        {
            return JsonConvert.ToElement(setName, value, schema);
        }

        protected void AttachAttributes(XElement value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            foreach (KeyValuePair<string, string> pair in nameValuePairs)
            {
                if (pair.Key == "schema" || pair.Key == "method") continue;
                value.SetAttributeValue(pair.Key, pair.Value);
            }
        }

        protected XElement GetSchema(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            string schemaName = nameValuePairs.GetValue("schema");
            XElement schema = ElementContext.GetSchema(schemaName);

            if (SpecifiedConfigGetter != null)
            {
                XElement specifiedConfig = GetSpecifiedConfig(nameValuePairs);
                schema.Modify(specifiedConfig);
            }

            return nameValuePairs.GetSchema(schema);
        }

        protected XElement GetSpecifiedConfig(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>(nameValuePairs);
            return SpecifiedConfigGetter.Get(list);
        }

        protected ElementQuerier GetElementQuerier(string accept)
        {
            if (string.IsNullOrWhiteSpace(accept) || accept == "xml") return new ElementQuerier(ElementContext);
            return new ElementQuerierEx(ElementContext);
        }


    }
}
