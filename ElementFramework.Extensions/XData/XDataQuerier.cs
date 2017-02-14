using System;
using System.Collections.Generic;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Schema;
using XData.Data.Extensions;
using XData.Data.Components;

namespace XData.Data.Element
{
    public partial class XDataQuerier
    {
        protected ElementContext ElementContext;
        protected SpecifiedConfigGetter SpecifiedConfigGetter;

        public XDataQuerier(ElementContext elementContext, SpecifiedConfigGetter specifiedConfigGetter)
        {
            ElementContext = elementContext;
            SpecifiedConfigGetter = specifiedConfigGetter;
        }

        public virtual XElement GetNow(string accept)
        {
            XElement result = new XElement("Now");
            if (accept == "xml")
            {
                result.Value = ElementContext.GetNow().ToCanonicalString();
                return result;
            }
            if (accept == "json")
            {
                string content = JsonConvert.DateTimeToJson("Now", ElementContext.GetNow());
                result.SetAttributeValue("Content", content);
            }
            else
            {
                result.Value = ElementContext.GetNow().ToCanonicalString();
                result.SetAttributeValue("DataType", typeof(DateTime).ToString());
            }
            result.SetAttributeValue("Accept", accept);
            return result;
        }

        public virtual XElement GetUtcNow(string accept)
        {
            XElement result = new XElement("Now");
            if (accept == "xml")
            {
                result.Value = ElementContext.GetUtcNow().ToCanonicalString();
                return result;
            }
            if (accept == "json")
            {
                string content = JsonConvert.DateTimeToJson("Now", ElementContext.GetUtcNow());
                result.SetAttributeValue("Content", content);
            }
            else
            {
                result.Value = ElementContext.GetNow().ToCanonicalString();
                result.SetAttributeValue("DataType", typeof(DateTime).ToString());
            }
            result.SetAttributeValue("Accept", accept);
            return result;

        }

        public virtual XElement GetDefault(string elementName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            string select = GetValue("$select", nameValuePairs);
            XElement schema = GetSchema(nameValuePairs);

            return GetDefault(elementName, select, accept, schema);
        }

        //<Config [Version="1.2.3"]>
        //    ...
        //</Config>
        public virtual XElement GetDefault(string elementName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            string select = GetValue("$select", nameValuePairs);
            XElement schema = GetSchema(nameValuePairs);
            schema.Modify(config);

            return GetDefault(elementName, select, accept, schema);
        }

        protected virtual XElement GetDefault(string elementName, string select, string accept, XElement schema)
        {
            // compatibility
            // elementName maybe is setName
            string element_Name = elementName;
            XElement elementSchema = schema.GetElementSchema(elementName);
            if (elementSchema == null)
            {
                element_Name = schema.GetElementSchemaBySetName(elementName).Name.LocalName;
            }

            ElementQuerier elementQuerier = GetElementQuerier(accept);
            XElement result = elementQuerier.GetDefault(element_Name, select, schema);
            result.SetAttributeValue("Accept", accept);
            return result;
        }

        public virtual XElement GetCount(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            string filter = GetValue("$filter", nameValuePairs);
            XElement schema = GetSchema(nameValuePairs);

            return GetCount(setName, filter, accept, schema);
        }

        public virtual XElement GetCount(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            string filter = GetValue("$filter", nameValuePairs);
            XElement schema = GetSchema(nameValuePairs);
            schema.Modify(config);

            return GetCount(setName, filter, accept, schema);
        }

        protected virtual XElement GetCount(string setName, string filter, string accept, XElement schema)
        {
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;
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

        public virtual XElement Find(string setNameWithKey, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            if (IsGetTreeOrTrees(nameValuePairs))
            {
                string[] key;
                string setName = GetSetName(setNameWithKey, out key);
                return GetTree(setName, key, nameValuePairs, accept);
            }

            //
            string select = GetValue("$select", nameValuePairs);
            string expand = GetValue("$expand", nameValuePairs);
            XElement schema = GetSchema(nameValuePairs);

            return Find(setNameWithKey, select, expand, accept, schema);
        }

        public virtual XElement Find(string setNameWithKey, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            if (IsGetTreeOrTrees(nameValuePairs))
            {
                string[] key;
                string setName = GetSetName(setNameWithKey, out key);
                return GetTree(setName, key, nameValuePairs, accept, config);
            }

            //
            string select = GetValue("$select", nameValuePairs);
            string expand = GetValue("$expand", nameValuePairs);
            XElement schema = GetSchema(nameValuePairs);
            schema.Modify(config);

            return Find(setNameWithKey, select, expand, accept, schema);
        }

        protected virtual XElement Find(string setNameWithKey, string select, string expand, string accept, XElement schema)
        {
            string[] key;
            string setName = GetSetName(setNameWithKey, out key);
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;

            ElementQuerier elementQuerier = GetElementQuerier(accept);

            XElement result;
            if (string.IsNullOrWhiteSpace(expand))
            {
                result = elementQuerier.Find(elementName, key, select, schema);
            }
            else
            {
                ExpandCollection expandCollection = new ExpandCollection(expand, elementName, schema);
                result = elementQuerier.Find(elementName, key, select, expandCollection, schema);
            }
            result.SetAttributeValue("Accept", accept);
            return result;
        }

        public virtual XElement GetSet(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            if (IsGetTreeOrTrees(nameValuePairs))
            {
                return GetTrees(setName, nameValuePairs, accept);
            }

            //
            string select = GetValue("$select", nameValuePairs);
            string filter = GetValue("$filter", nameValuePairs);
            string orderby = GetValue("$orderby", nameValuePairs);
            string top = GetValue("$top", nameValuePairs);
            string skip = GetValue("$skip", nameValuePairs);
            string expand = GetValue("$expand", nameValuePairs);
            XElement schema = GetSchema(nameValuePairs);

            return GetSet(setName, select, filter, orderby, skip, top, expand, accept, schema);
        }

        public virtual XElement GetSet(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            if (IsGetTreeOrTrees(nameValuePairs))
            {
                return GetTrees(setName, nameValuePairs, accept, config);
            }

            //
            string select = GetValue("$select", nameValuePairs);
            string filter = GetValue("$filter", nameValuePairs);
            string orderby = GetValue("$orderby", nameValuePairs);
            string top = GetValue("$top", nameValuePairs);
            string skip = GetValue("$skip", nameValuePairs);
            string expand = GetValue("$expand", nameValuePairs);
            XElement schema = GetSchema(nameValuePairs);
            schema.Modify(config);

            return GetSet(setName, select, filter, orderby, skip, top, expand, accept, schema);
        }

        protected virtual XElement GetSet(string setName, string select, string filter, string orderby, string skip, string top, string expand, string accept, XElement schema)
        {
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;

            ElementQuerier elementQuerier = GetElementQuerier(accept);

            XElement result;
            if (string.IsNullOrWhiteSpace(expand))
            {
                if (string.IsNullOrWhiteSpace(top) && string.IsNullOrWhiteSpace(skip))
                {
                    if (accept == "json")
                    {
                        result = new XElement(setName);
                        IEnumerable<string> results = new JsonQuerier(ElementContext).GetSet(elementName, select, filter, orderby, schema);
                        string content = string.Join(",", results);
                        content = string.Format("[{0}]", content);
                        result.SetAttributeValue("Content", content);
                    }
                    else
                    {
                        result = elementQuerier.GetSet(elementName, select, filter, orderby, schema);
                    }
                }
                else
                {
                    int iSkip = int.Parse(skip);
                    int take = int.Parse(top);
                    if (accept == "json")
                    {
                        result = new XElement(setName);
                        IEnumerable<string> results = new JsonQuerier(ElementContext).GetPage(elementName, select, filter, orderby, iSkip, take, schema);
                        string content = string.Join(",", results);
                        content = string.Format("[{0}]", content);
                        result.SetAttributeValue("Content", content);
                    }
                    else
                    {
                        result = elementQuerier.GetPage(elementName, select, filter, orderby, iSkip, take, schema);
                    }
                }
            }
            else
            {
                // XData/Users?$expand=Trips($filter=Name eq 'Trip' $select=Id,Name $orderby=Id,Name $expand=Hotels($expand=Addrs)),Contacts
                ExpandCollection expandCollection = new ExpandCollection(expand, elementName, schema);
                if (string.IsNullOrWhiteSpace(top) && string.IsNullOrWhiteSpace(skip))
                {
                    result = elementQuerier.GetSet(elementName, select, filter, orderby, expandCollection, schema);
                }
                else
                {
                    int iSkip = int.Parse(skip);
                    int take = int.Parse(top);
                    result = elementQuerier.GetPage(elementName, select, filter, orderby, iSkip, take, expandCollection, schema);
                }
            }
            result.SetAttributeValue("Accept", accept);
            return result;
        }

        protected static string GetValue(string key, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return nameValuePairs.GetValue(key);
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

        protected ElementQuerier GetElementQuerier(string accept)
        {
            if (string.IsNullOrWhiteSpace(accept) || accept == "xml") return new ElementQuerier(ElementContext);
            return new ElementQuerierEx(ElementContext);
        }

        protected string GetSetName(string setNameWithKey, out string[] key)
        {
            int index = setNameWithKey.IndexOf('(');
            string setName = setNameWithKey.Substring(0, index).Trim();
            string keyStr = setNameWithKey.Substring(index).Trim();
            keyStr = keyStr.TrimStart('(').TrimEnd(')');
            string[] keyValues = keyStr.Split(',');
            for (int i = 0; i < keyValues.Length; i++)
            {
                keyValues[i] = keyValues[i].Trim();
            }
            key = keyValues;
            return setName;
        }


    }
}
