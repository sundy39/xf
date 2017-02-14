using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Schema;
using XData.Data.Extensions;

namespace XData.Data.Element
{
    public partial class XDataQuerier
    {
        public bool IsGetTreeOrTrees(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            string value = GetValue("struct", nameValuePairs);
            return value == "tree" || value == "Tree";
        }

        // overload
        public virtual XElement GetTree(string setName, string[] key, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            string parentfields = GetValue("parentfields", nameValuePairs);
            string[] parentFieldNames = string.IsNullOrWhiteSpace(parentfields) ? null : parentfields.Split(',');
            string select = GetValue("$select", nameValuePairs);
            string orderby = GetValue("$orderby", nameValuePairs);
            string omittedsetnodes = GetValue("omittedsetnodes", nameValuePairs);
            bool omittedSetNodes = omittedsetnodes == "true" || omittedsetnodes == "True";
            XElement schema = GetSchema(nameValuePairs);
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;
            return GetTree(elementName, key, parentFieldNames, select, orderby, omittedSetNodes, accept, schema);
        }

        // overload
        public virtual XElement GetTree(string setName, string[] key, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            string parentfields = GetValue("parentfields", nameValuePairs);
            string[] parentFieldNames = string.IsNullOrWhiteSpace(parentfields) ? null : parentfields.Split(',');
            string select = GetValue("$select", nameValuePairs);
            string orderby = GetValue("$orderby", nameValuePairs);
            string omittedsetnodes = GetValue("omittedsetnodes", nameValuePairs);
            bool omittedSetNodes = omittedsetnodes == "true" || omittedsetnodes == "True";
            XElement schema = GetSchema(nameValuePairs);
            schema.Modify(config);
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;
            return GetTree(elementName, key, parentFieldNames, select, orderby, omittedSetNodes, accept, schema);
        }

        public virtual XElement GetTree(string elementName, string[] key, string[] parentFieldNames, string select, string orderby, bool omittedSetNodes, string accept, XElement schema)
        {
            XElement elementSchema = schema.GetElementSchema(elementName);
            string[] parentFields = GetParentFieldNamesOrDefault(parentFieldNames, elementSchema);
            ElementQuerier elementQuerier = GetElementQuerier(accept);
            XElement root;
            if (string.IsNullOrWhiteSpace(select))
            {
                root = elementQuerier.Find(elementName, key, null, schema);
                IEnumerable<XElement> elements = elementQuerier.GetSet(elementName, null, null, orderby, schema).Elements();
                elements.BuildTree(root, parentFields, elementSchema, omittedSetNodes);
            }
            else
            {
                IEnumerable<string> excepted;
                IEnumerable<string> fieldNames = GetSelectWithKeyAndParentFieldNames(select, elementSchema.GetKeySchema(), parentFields, out excepted);
                string selectEx = string.Join(",", fieldNames);

                root = elementQuerier.Find(elementName, key, selectEx, schema);
                IEnumerable<XElement> elements = elementQuerier.GetSet(elementName, selectEx, null, orderby, schema).Elements();
                elements.BuildTree(root, parentFields, elementSchema, omittedSetNodes);

                RemoveExcepted(root.DescendantsAndSelf(elementName), excepted);
            }

            root.SetAttributeValue("Accept", accept);
            return root;
        }

        private string[] GetParentFieldNamesOrDefault(string[] parentFieldNames, XElement elementSchema)
        {
            if (parentFieldNames == null || parentFieldNames.Length == 0)
            {
                string[] result = null;
                XElement keySchema = elementSchema.GetKeySchema();
                if (keySchema.Elements().Count() == 1)
                {
                    string keyFieldName = keySchema.Elements().First().Name.LocalName;
                    IEnumerable<XElement> fields = elementSchema.Elements()
                        .Where(x => x.Name.LocalName != keyFieldName)
                        .Where(x =>
                        x.Name.LocalName.Contains("Parent") ||
                        x.Name.LocalName.Contains("PARENT") ||
                        x.Name.LocalName.Contains("parent"));
                    if (fields.Count() == 1)
                    {
                        return new string[] { fields.First().Name.LocalName };
                    }
                }
                return result;
            }
            return parentFieldNames;
        }

        private IEnumerable<string> GetSelectWithKeyAndParentFieldNames(string select, XElement keySchema, string[] parentFieldNames, out IEnumerable<string> excepted)
        {
            IEnumerable<string> selectFieldNames = select.Split(',').Select(s => s.Trim());

            List<string> list = new List<string>(selectFieldNames);
            list.AddRange(keySchema.Elements().Select(x => x.Name.LocalName));
            list.AddRange(parentFieldNames.Select(s => s.Trim()));
            IEnumerable<string> fieldNames = list.Distinct();
            excepted = fieldNames.Except(selectFieldNames);
            return fieldNames;
        }

        private void RemoveExcepted(IEnumerable<XElement> elements, IEnumerable<string> excepted)
        {
            foreach (XElement element in elements)
            {
                foreach (string fieldName in excepted)
                {
                    element.Element(fieldName).Remove();
                }
            }
        }

        // overload
        public virtual XElement GetTrees(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            string parentfields = GetValue("parentfields", nameValuePairs);
            string[] parentFieldNames = string.IsNullOrWhiteSpace(parentfields) ? null : parentfields.Split(',');
            string select = GetValue("$select", nameValuePairs);
            string orderby = GetValue("$orderby", nameValuePairs);
            string omittedsetnodes = GetValue("omittedsetnodes", nameValuePairs);
            bool omittedSetNodes = omittedsetnodes == "true" || omittedsetnodes == "True";
            XElement schema = GetSchema(nameValuePairs);
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;
            return GetTrees(elementName, parentFieldNames, select, orderby, omittedSetNodes, accept, schema);
        }

        // overload
        public virtual XElement GetTrees(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            string parentfields = GetValue("parentfields", nameValuePairs);
            string[] parentFieldNames = string.IsNullOrWhiteSpace(parentfields) ? null : parentfields.Split(',');
            string select = GetValue("$select", nameValuePairs);
            string orderby = GetValue("$orderby", nameValuePairs);
            string omittedsetnodes = GetValue("omittedsetnodes", nameValuePairs);
            bool omittedSetNodes = omittedsetnodes == "true" || omittedsetnodes == "True";
            XElement schema = GetSchema(nameValuePairs);
            schema.Modify(config);
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;
            return GetTrees(elementName, parentFieldNames, select, orderby, omittedSetNodes, accept, schema);
        }

        public virtual XElement GetTrees(string elementName, string[] parentFieldNames, string select, string orderby, bool omittedSetNodes, string accept, XElement schema)
        {
            XElement elementSchema = schema.GetElementSchema(elementName);
            string[] parentFields = GetParentFieldNamesOrDefault(parentFieldNames, elementSchema);
            ElementQuerier elementQuerier = GetElementQuerier(accept);
            XElement result;
            if (string.IsNullOrWhiteSpace(select))
            {
                IEnumerable<XElement> elements = elementQuerier.GetSet(elementName, null, null, orderby, schema).Elements();
                result = elements.ExtractTrees(parentFields, elementSchema, omittedSetNodes);
            }
            else
            {
                IEnumerable<string> excepted;
                IEnumerable<string> fieldNames = GetSelectWithKeyAndParentFieldNames(select, elementSchema.GetKeySchema(), parentFields, out excepted);

                IEnumerable<XElement> elements = elementQuerier.GetSet(elementName, string.Join(",", fieldNames), null, orderby, schema).Elements();
                result = elements.ExtractTrees(parentFields, elementSchema, omittedSetNodes);

                RemoveExcepted(result.Descendants(elementName), excepted);
            }

            result.SetAttributeValue("Accept", accept);
            return result;
        }


    }
}
