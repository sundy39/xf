using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public class ElementQuerier
    {
        protected ElementContext ElementContext { get; private set; }

        public ElementQuerier(ElementContext elementContext)
        {
            ElementContext = elementContext;
        }

        public DateTime GetNow()
        {
            return ElementContext.GetNow();
        }

        public DateTime GetUtcNow()
        {
            return ElementContext.GetUtcNow();
        }

        // overload
        public XElement GetDefault(string elementName)
        {
            return GetDefault(elementName, null);
        }

        // overload
        public XElement GetDefault(string elementName, string select)
        {
            return GetDefault(elementName, select, ElementContext.PrimarySchema);
        }

        // overload
        public XElement GetDefault(string elementName, string select, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetDefault(elementName, select, schema);
        }

        public virtual XElement GetDefault(string elementName, string select, XElement schema)
        {
            XElement referenceSchema;
            XElement query = GetQuery(elementName, select, null, null, schema, out referenceSchema);
            XElement modifying = new XElement("Config");
            modifying.Add(referenceSchema);

            XElement newSchema = new XElement(schema);
            newSchema.Modify(modifying);

            return ElementContext.GetDefault(query, newSchema);
        }

        public int GetCount(string elementName)
        {
            return ElementContext.GetCount(elementName);
        }

        // overload
        public int GetCount(string elementName, string filter)
        {
            return GetCount(elementName, filter, ElementContext.PrimarySchema);
        }

        // overload
        public int GetCount(string elementName, string filter, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetCount(elementName, filter, schema);
        }

        public int GetCount(string elementName, string filter, XElement schema)
        {
            XElement referenceSchema;
            XElement query = GetQuery(elementName, null, filter, null, schema, out referenceSchema);
            XElement modifying = new XElement("Config");
            modifying.Add(referenceSchema);

            XElement newSchema = new XElement(schema);
            newSchema.Modify(modifying);

            return ElementContext.GetCount(query, newSchema);
        }

        // overload
        public XElement Find(string elementName, string[] key)
        {
            return Find(elementName, key, string.Empty);
        }

        // overload
        public XElement Find(string elementName, string[] key, string select)
        {
            return Find(elementName, key, select, ElementContext.PrimarySchema);
        }

        // overload
        public XElement Find(string elementName, string[] key, string select, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return Find(elementName, key, select, schema);
        }

        public virtual XElement Find(string elementName, string[] key, string select, XElement schema)
        {
            string filter = GetFilter(elementName, key, schema);
            XElement referenceSchema;
            XElement query = GetQuery(elementName, select, filter, null, schema, out referenceSchema);
            XElement modifying = new XElement("Config");
            modifying.Add(referenceSchema);

            XElement newSchema = new XElement(schema);
            newSchema.Modify(modifying);

            XElement result = ElementContext.GetSet(query, newSchema);
            if (result.HasElements) return result.Elements().First();
            return new XElement(elementName);
        }

        // overload
        public XElement Find(string elementName, string[] key, IEnumerable<Expand> expands)
        {
            return Find(elementName, key, null, expands, ElementContext.PrimarySchema);
        }

        // overload
        public XElement Find(string elementName, string[] key, string select, IEnumerable<Expand> expands)
        {
            return Find(elementName, key, select, expands, ElementContext.PrimarySchema);
        }

        // overload
        public XElement Find(string elementName, string[] key, string select, IEnumerable<Expand> expands, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return Find(elementName, key, select, expands, schema);
        }

        public virtual XElement Find(string elementName, string[] key, string select, IEnumerable<Expand> expands, XElement schema)
        {
            string filter = GetFilter(elementName, key, schema);
            XElement referenceSchema;
            XElement query = GetQuery(elementName, select, filter, null, schema, out referenceSchema);

            IEnumerable<XElement> expandReferenceSchemas;
            XElement expandsSchema = GetExpandsSchema(elementName, expands, schema, out expandReferenceSchemas);

            XElement newSchema = MergeSchemas(schema, referenceSchema, expandReferenceSchemas, expandsSchema);

            XElement result = ElementContext.GetSet(query, newSchema);
            if (result.HasElements) return result.Elements().First();
            return new XElement(elementName);
        }

        // overload
        public XElement GetSet(string elementName, string select, string filter, string orderBy)
        {
            return GetSet(elementName, select, filter, orderBy, ElementContext.PrimarySchema);
        }

        // overload
        public XElement GetSet(string elementName, string select, string filter, string orderby, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetSet(elementName, select, filter, orderby, schema);
        }

        public virtual XElement GetSet(string elementName, string select, string filter, string orderby, XElement schema)
        {
            XElement referenceSchema;
            XElement query = GetQuery(elementName, select, filter, orderby, schema, out referenceSchema);
            XElement modifying = new XElement("Config");
            modifying.Add(referenceSchema);

            XElement newSchema = new XElement(schema);
            newSchema.Modify(modifying);

            return ElementContext.GetSet(query, newSchema);
        }

        // overload
        public XElement GetSet(string elementName, string select, string filter, string orderBy, IEnumerable<Expand> expands)
        {
            return GetSet(elementName, select, filter, orderBy, expands, ElementContext.PrimarySchema);
        }

        // overload
        public XElement GetSet(string elementName, string select, string filter, string orderBy, IEnumerable<Expand> expands, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetSet(elementName, select, filter, orderBy, expands, schema);
        }

        public virtual XElement GetSet(string elementName, string select, string filter, string orderBy, IEnumerable<Expand> expands, XElement schema)
        {
            XElement referenceSchema;
            XElement query = GetQuery(elementName, select, filter, orderBy, schema, out referenceSchema);

            IEnumerable<XElement> expandReferenceSchemas;
            XElement expandsSchema = GetExpandsSchema(elementName, expands, schema, out expandReferenceSchemas);

            XElement newSchema = MergeSchemas(schema, referenceSchema, expandReferenceSchemas, expandsSchema);

            return ElementContext.GetSet(query, newSchema);
        }

        // overload
        public XElement GetPage(string elementName, string select, string filter, string orderBy, int skip, int take)
        {
            return GetPage(elementName, select, filter, orderBy, skip, take, ElementContext.PrimarySchema);
        }

        // overload
        public XElement GetPage(string elementName, string select, string filter, string orderby, int skip, int take, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetPage(elementName, select, filter, orderby, skip, take, schema);
        }

        public virtual XElement GetPage(string elementName, string select, string filter, string orderby, int skip, int take, XElement schema)
        {
            XElement referenceSchema;
            XElement query = GetQuery(elementName, select, filter, orderby, skip, take, schema, out referenceSchema);
            XElement modifying = new XElement("Config");
            modifying.Add(referenceSchema);

            XElement newSchema = new XElement(schema);
            newSchema.Modify(modifying);

            return ElementContext.GetSet(query, newSchema);
        }

        // overload
        public XElement GetPage(string elementName, string select, string filter, string orderby, int skip, int take, IEnumerable<Expand> expands)
        {
            return GetPage(elementName, select, filter, orderby, skip, take, expands, ElementContext.PrimarySchema);
        }

        // overload
        public XElement GetPage(string elementName, string select, string filter, string orderby, int skip, int take, IEnumerable<Expand> expands, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetPage(elementName, select, filter, orderby, skip, take, expands, schema);
        }

        public virtual XElement GetPage(string elementName, string select, string filter, string orderby, int skip, int take, IEnumerable<Expand> expands, XElement schema)
        {
            XElement referenceSchema;
            XElement query = GetQuery(elementName, select, filter, orderby, skip, take, schema, out referenceSchema);

            IEnumerable<XElement> expandReferenceSchemas;
            XElement expandsSchema = GetExpandsSchema(elementName, expands, schema, out expandReferenceSchemas);

            XElement newSchema = MergeSchemas(schema, referenceSchema, expandReferenceSchemas, expandsSchema);

            return ElementContext.GetSet(query, newSchema);
        }

        protected string GetFilter(string elementName, string[] key, XElement schema)
        {
            XElement elementSchema = schema.GetElementSchema(elementName);
            XElement keySchema = elementSchema.GetKeySchema();
            XElement[] fieldSchemas = keySchema.Elements().ToArray();
            List<string> list = new List<string>();
            for (int i = 0; i < fieldSchemas.Length; i++)
            {
                XElement fieldSchema = fieldSchemas[i];
                Type type = Type.GetType(fieldSchema.Attribute("DataType").Value);
                string value;
                if (type == typeof(string))
                {
                    value = string.Format("'{0}'", key[i]);
                }
                else if (type == typeof(DateTime))
                {
                    value = string.Format("datetime'{0}'", key[i]);
                }
                else
                {
                    value = key[i];
                }
                list.Add(fieldSchema.Name.LocalName + " eq " + value);
            }
            return string.Join(" and ", list.ToArray());
        }

        protected XElement GetQuery(string elementName, string select, string filter, string orderby, XElement schema, out XElement referenceSchema)
        {
            XElement query = new XElement(elementName);
            List<string> fields = new List<string>();
            if (string.IsNullOrWhiteSpace(select))
            {
                XElement xSelect = new XElement("Select");
                XElement elementSchema = schema.GetElementSchema(elementName);
                foreach (XElement fieldSchema in elementSchema.Elements())
                {
                    if (fieldSchema.Attribute(Glossary.Element) == null)
                    {
                        xSelect.Add(new XElement(fieldSchema.Name));
                        fields.Add(fieldSchema.Name.LocalName);
                    }
                }
                query.Add(xSelect);
            }
            else
            {
                XElement xSelect = new Select(select).ToElement();
                query.Add(xSelect);
                fields.AddRange(xSelect.Elements().Select(x => x.Name.LocalName));
            }

            //
            if (!string.IsNullOrWhiteSpace(filter))
            {
                XElement xFilter = new XElement("Filter");
                xFilter.FillFilter(filter, elementName, schema);
                query.Add(xFilter);
                fields.AddRange(xFilter.Element("Fields").Elements().Select(x => x.Name.LocalName));
            }

            //
            if (!string.IsNullOrWhiteSpace(orderby))
            {
                XElement[] xOrderBy = new OrderBy(orderby).ToElements();
                query.Add(xOrderBy);
                fields.AddRange(xOrderBy.Select(x => x.Elements().First().Name.LocalName));
            }

            //
            fields = fields.Distinct().Where(s => s.Contains('.')).ToList();
            referenceSchema = new XElement(elementName);
            foreach (string filed in fields)
            {
                referenceSchema.Add(GetReferenceFieldSchema(elementName, filed, schema));
            }
            return query;
        }

        protected XElement GetReferenceFieldSchema(string elementName, string referenceFieldName, XElement schema)
        {
            int index = referenceFieldName.LastIndexOf('.');
            string rElement = referenceFieldName.Substring(0, index);
            string rField = referenceFieldName.Substring(index + 1);

            if (schema.Element(rElement) == null) return null;
            if (schema.Element(rElement).Element(rField) == null) return null;

            XElement rFieldSchema = new XElement(referenceFieldName);
            rFieldSchema.SetAttributeValue("Element", rElement);
            rFieldSchema.SetAttributeValue("Field", rField);

            // Prime
            SimpleRelationship toOneRelationship = schema.CreatePrimeToOneRelationship(elementName, rElement);
            if (toOneRelationship != null)
            {
                rFieldSchema.SetAttributeValue("Relationship.Content", toOneRelationship.Content);
                return rFieldSchema;
            }

            // ToMany
            Relationship relationship = schema.CreatePrimeToOneRelationship(rElement, elementName);
            if (relationship != null)
            {
                Debug.Assert(relationship is ManyToOneRelationship);

                rFieldSchema.SetAttributeValue("Relationship.Content", relationship.Reverse().Content);
                return rFieldSchema;
            }

            //
            ManyToManyRelationship manyToManyRelationship = schema.CreatePrimeManyToManyRelationship(elementName, rElement);
            if (manyToManyRelationship != null)
            {
                rFieldSchema.SetAttributeValue("ReferencePath.Content", manyToManyRelationship.Content);
                return rFieldSchema;
            }

            XElement referencePathSchema = schema.GetPrimeReferencePathSchema(elementName, rElement);
            if (referencePathSchema != null)
            {
                rFieldSchema.SetAttributeValue("ReferencePath.Content", referencePathSchema.Attribute("Content").Value);
                return rFieldSchema;
            }

            // 
            toOneRelationship = schema.CreateToOneRelationship(elementName, rElement);
            if (toOneRelationship != null)
            {
                rFieldSchema.SetAttributeValue("Relationship.Content", toOneRelationship.Content);
                return rFieldSchema;
            }

            referencePathSchema = schema.GetReferencePathSchema(elementName, rElement);
            if (referencePathSchema != null)
            {
                rFieldSchema.SetAttributeValue("ReferencePath.Content", referencePathSchema.Attribute("Content").Value);
                return rFieldSchema;
            }

            // ToMany
            relationship = schema.CreateRelationship(elementName, rElement);
            if (relationship != null)
            {
                rFieldSchema.SetAttributeValue("ReferencePath.Content", relationship.Content);
                return rFieldSchema;
            }

            // RelationshipPath
            IEnumerable<XElement> relationshipPaths = schema.Elements(Glossary.RelationshipPath).Where(x => x.Attribute(Glossary.RelationshipContent) != null &&
                    x.Attribute(Glossary.RelationshipFrom).Value == elementName && x.Attribute(Glossary.RelationshipTo).Value == rElement);
            if (relationshipPaths.Count() == 1)
            {
                rFieldSchema.SetAttributeValue("ReferencePath.Content", relationshipPaths.First().Attribute("Content").Value);
                return rFieldSchema;
            }

            // InferReferencePath
            XElement relationshipPath = schema.InferReferencePath(elementName, rElement);
            if (relationshipPath != null)
            {
                rFieldSchema.SetAttributeValue("ReferencePath.Content", relationshipPath.Attribute("Content").Value);
                return rFieldSchema;
            }

            return rFieldSchema;
        }

        protected XElement GetQuery(string elementName, string select, string filter, string orderby, int skip, int take, XElement schema, out XElement referenceSchema)
        {
            XElement query = GetQuery(elementName, select, filter, orderby, schema, out referenceSchema);

            XElement clause = new XElement("Skip");
            clause.Add(new XElement("Count", skip));
            query.Add(clause);

            clause = new XElement("Take");
            clause.Add(new XElement("Count", take));
            query.Add(clause);

            return query;
        }

        protected XElement GetExpandsSchema(string elementName, IEnumerable<Expand> expands, XElement schema, out IEnumerable<XElement> expandReferenceSchemas)
        {
            List<XElement> expandSchemas = new List<XElement>();
            List<XElement> erSchemas = new List<XElement>();
            foreach (Expand expand in expands)
            {
                XElement fieldSchema = expand.GenerateSchema(schema);
                expandSchemas.Add(fieldSchema);
                GenerateExpandReferenceSchema(expand, erSchemas, schema);
            }
            expandReferenceSchemas = erSchemas;
            return new XElement(elementName, expandSchemas);
        }

        protected void GenerateExpandReferenceSchema(Expand expand, List<XElement> expandReferenceSchemas, XElement schema)
        {
            XElement referenceSchema;
            GetQuery(expand.Element, expand.Select, expand.Filter, expand.OrderBy, schema, out referenceSchema);
            expandReferenceSchemas.Add(referenceSchema);
            foreach (Expand child in expand.Expands)
            {
                GenerateExpandReferenceSchema(child, expandReferenceSchemas, schema);
            }
        }

        protected XElement MergeSchemas(XElement schema, XElement referenceSchema, IEnumerable<XElement> expandReferenceSchemas, XElement expandsSchema)
        {
            XElement newSchema = new XElement(schema);

            XElement modifying = new XElement("Config");
            modifying.Add(referenceSchema);
            newSchema.Modify(modifying);

            modifying = new XElement("Config");
            modifying.Add(expandReferenceSchemas);
            newSchema.Modify(modifying);

            modifying = new XElement("Config");
            modifying.Add(expandsSchema);
            newSchema.Modify(modifying);

            return newSchema;
        }


    }
}
