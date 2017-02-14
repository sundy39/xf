using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Query;
using XData.Data.Schema;
using XData.Data.Resources;

namespace XData.Data.Objects
{
    public partial class Reader
    {
        protected readonly Database Database;

        public Reader(Database database)
        {
            Database = database;
        }

        // Element("Select")
        public XElement GetDefault(XElement query, XElement schema)
        {
            XElement element = new XElement(query.Name);
            element.Add(query.Element("Select"));
            FillUpSelect(element, schema);
            XElement result = Database.GetDefault(element, schema);

            //
            XElement elementSchema = schema.GetElementSchema(query.Name.LocalName);
            foreach (XElement fieldSchema in elementSchema.Elements().Where(p =>
                p.Attribute(Glossary.Element) != null && p.Attribute(Glossary.Field) != null))
            {
                if (result.Element(fieldSchema.Name) == null) continue;

                if (fieldSchema.Element("DefaultValue") != null)
                {
                    string value = fieldSchema.Element("DefaultValue").Element("Value").Value;
                    if (value == "DateTime.Now")
                    {
                        value = Database.GetNow().ToCanonicalString();
                    }
                    else if (value == "DateTime.UtcNow")
                    {
                        value = Database.GetUtcNow().ToCanonicalString();
                    }
                    result.Element(fieldSchema.Name).Value = value;
                    if (schema.Attribute("Ex") != null)
                    {
                        string dataType = fieldSchema.Attribute("DataType").Value;
                        result.Element(fieldSchema.Name).SetAttributeValue("DataType", dataType);
                    }
                }
            }
            return result;
        }

        // Element("Where") or Element("Filter")
        public int GetCount(XElement query, XElement schema)
        {
            if (query.Element("Filter") == null)
            {
                XElement element = new XElement(query.Name);
                element.Add(query.Element("Where"));
                return Database.GetCount(element, schema);
            }
            return Database.GetCount(query, schema);
        }

        // List, Page
        public XElement GetSet(XElement query, XElement schema)
        {
            FillUpSelect(query, schema);

            XElement elementSchema = schema.GetElementSchema(query.Name.LocalName);
            if (elementSchema.Elements().Any(p => p.Attribute(Glossary.Element) != null && p.Attribute(Glossary.Field) == null))
            {
                return GetSetWithRelatedObjects(query, schema);
            }

            return Database.GetSet(query, schema);
        }

        internal IEnumerable<T> GetSet<T>(XElement query, XElement schema, IFastGetter<T> fastGetter)
        {
            return Database.GetSet(query, schema, fastGetter);
        }

        // SELECT *
        protected void FillUpSelect(XElement query, XElement schema)
        {
            List<string> list = new List<string>();

            XElement select;
            if (query.Element("Select") == null)
            {
                select = new XElement("Select");
                query.Add(select);
            }
            else
            {
                select = query.Element("Select");
                if (select.HasElements) return;
            }
            XElement elementSchema = schema.GetElementSchema(query.Name.LocalName);
            select.Add(elementSchema.GetEmptyElement().Elements());
        }

        //
        protected XElement GetSetWithRelatedObjects(XElement query, XElement schema)
        {
            IEnumerable<RelatedObject> objects;
            IEnumerable<string> elementAsParentFields;

            objects = schema.GetRelatedObjects(query.Name.LocalName);
            elementAsParentFields = objects.GetElementAsParentFields();

            // try add fields of FK
            List<string> additional = new List<string>();
            foreach (RelatedObject relatedObject in objects)
            {
                string[] fieldNames = relatedObject.RelationshipPath.Relationships[0].FieldNames;
                additional.AddRange(fieldNames);
            }
            additional = additional.Distinct().ToList();
            foreach (XElement field in query.Element("Select").Elements())
            {
                if (additional.Contains(field.Name.LocalName))
                {
                    additional.Remove(field.Name.LocalName);
                }
            }
            foreach (string field in additional)
            {
                query.Element("Select").Add(new XElement(field));
            }

            //
            XElement result = GetSetWithAttributes(elementAsParentFields, query, schema);

            //
            foreach (XElement element in result.Elements())
            {
                foreach (RelatedObject obj in objects)
                {
                    ElementTexturer texturer = new ElementTexturer(obj, element, schema, Database);
                    XElement objResult = texturer.GetElement();
                    element.Add(objResult);
                }

                //
                element.RemoveAttributes();

                //
                foreach (string field in additional)
                {
                    element.Element(field).Remove();
                }
            }
            return result;
        }

        protected XElement GetSetWithAttributes(IEnumerable<string> elementAsParentFields, XElement query, XElement schema)
        {
            XElement newQuery = new XElement(query);
            XElement select = newQuery.Element("Select");
            List<string> additionalFieldList = new List<string>();
            foreach (string fieldName in elementAsParentFields)
            {
                if (select.Element(fieldName) == null)
                {
                    select.Add(new XElement(fieldName));
                    additionalFieldList.Add(fieldName);
                }
            }
            XElement result = Database.GetSet(newQuery, schema);
            foreach (XElement element in result.Elements())
            {
                foreach (string fieldName in elementAsParentFields)
                {
                    element.SetAttributeValue(fieldName, element.Element(fieldName).Value);
                }

                //
                foreach (string fieldName in additionalFieldList)
                {
                    element.Element(fieldName).Remove();
                }
            }
            return result;
        }


    }
}
