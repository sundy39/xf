using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Element.Validation;
using XData.Data.Objects;
using XData.Data.Schema;
using XData.Data.Extensions;
using XData.Data.Components;

namespace XData.Data.Element
{
    public class XDataSubmitter
    {
        protected ElementContext ElementContext;
        protected SpecifiedConfigGetter SpecifiedConfigGetter;
        protected ElementSubmitter ElementSubmitter;

        public XDataSubmitter(ElementContext elementContext, SpecifiedConfigGetter specifiedConfigGetter)
        {
            ElementContext = elementContext;
            SpecifiedConfigGetter = specifiedConfigGetter;
            ElementSubmitter = new ElementSubmitter(ElementContext);
        }

        public XElement Create(XElement value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            AttachAttributes(value, nameValuePairs);

            XElement schema = GetSchema(nameValuePairs);
            bool isValidate = IsValidate(nameValuePairs);
            return ElementSubmitter.Create(value, schema, isValidate);
        }

        public XElement Delete(XElement value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            AttachAttributes(value, nameValuePairs);

            XElement schema = GetSchema(nameValuePairs);
            bool isValidate = IsValidate(nameValuePairs);
            return ElementSubmitter.Delete(value, schema, isValidate);
        }

        public XElement Update(XElement value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            AttachAttributes(value, nameValuePairs);

            XElement schema = GetSchema(nameValuePairs);
            bool isValidate = IsValidate(nameValuePairs);
            return ElementSubmitter.Update(value, schema, isValidate);
        }

        public XElement Submit(XElement packet, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            bool isValidate = IsValidate(nameValuePairs);
            return ElementSubmitter.Submit(packet, isValidate);
        }

        public XElement Create(string set, object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            XElement schema = GetSchema(nameValuePairs);           
            XElement element = ToElement(set, value, schema);
            AttachAttributes(element, nameValuePairs);

            bool isValidate = IsValidate(nameValuePairs);
            return ElementSubmitter.Create(element, schema, isValidate);
        }

        public XElement Delete(string set, object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            XElement schema = GetSchema(nameValuePairs);          
            XElement element = ToElement(set, value, schema);
            AttachAttributes(element, nameValuePairs);

            bool isValidate = IsValidate(nameValuePairs);
            return ElementSubmitter.Delete(element, schema, isValidate);
        }

        public XElement Update(string set, object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            XElement schema = GetSchema(nameValuePairs);          
            XElement element = ToElement(set, value, schema);
            AttachAttributes(element, nameValuePairs);

            bool isValidate = IsValidate(nameValuePairs);
            return ElementSubmitter.Update(element, schema, isValidate);
        }

        protected XElement ToElement(string set, object value, XElement schema)
        {
            return JsonConvert.ToElement(set, value, schema);
        }

        public XElement Submit(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            XElement packet = JsonConvert.ToPacket(value, GetSchema);
            bool isValidate = IsValidate(nameValuePairs);
            return ElementSubmitter.Submit(packet, isValidate);
        }

        private XElement GetSchema(XElement unit,  XElement config)
        {
            XAttribute attr = unit.Attribute("Schema");
            string schemaName = (attr == null) ? null : attr.Value;
            XElement schema = ElementContext.GetSchema(schemaName);

            if (SpecifiedConfigGetter != null)
            {
                List<KeyValuePair<string, string>> nameValuePairs = new List<KeyValuePair<string, string>>();
                XElement value = unit.Element("Current").Elements().First();
                foreach (XAttribute xAttr in value.Attributes())
                {
                    if (xAttr.Name.LocalName == "Method" ||
                        xAttr.Name.LocalName == "Resource" ||
                        xAttr.Name.LocalName == "Schema") continue;

                    nameValuePairs.Add(new KeyValuePair<string, string>(xAttr.Name.LocalName, xAttr.Value));
                }

                XElement specifiedConfig = SpecifiedConfigGetter.Get(nameValuePairs);
                schema.Modify(specifiedConfig);
            }

            schema.Modify(config);
            return schema;
        }

        protected XElement GetSchema(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            string schemaName = nameValuePairs.GetValue("schema");
            XElement schema = ElementContext.GetSchema(schemaName);

            if (SpecifiedConfigGetter != null)
            {
                XElement specifiedConfig = SpecifiedConfigGetter.Get(nameValuePairs);
                schema.Modify(specifiedConfig);
            }

            return nameValuePairs.GetSchema(schema);
        }

        protected bool IsValidate(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            string method = nameValuePairs.GetValue("method");
            return method == "Validate";
        }

        protected void AttachAttributes(XElement value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {    
            foreach (KeyValuePair<string, string> pair in nameValuePairs)
            {
                if (pair.Key == "schema" || pair.Key == "method") continue;
                value.SetAttributeValue(pair.Key, pair.Value);
            }     
        }


    }
}
