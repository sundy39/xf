using System;
using System.Collections.Generic;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Query;

namespace XData.Data.Element
{
    public partial class ElementContext
    {
        protected Reader Reader { get; private set; }

        public DateTime GetNow()
        {
            return Database.GetNow();
        }

        public DateTime GetUtcNow()
        {
            return Database.GetUtcNow();
        }

        // Element"(Select")
        // overload
        public XElement GetDefault(XElement query)
        {
            return GetDefault(query, PrimarySchema);
        }

        public XElement GetDefault(XElement query, XElement schema)
        {
            return Reader.GetDefault(query, schema);
        }

        // overload
        public XElement GetDefault(XElement query, string schemaName)
        {
            XElement schema = GetSchema(schemaName);
            return GetDefault(query, schema);
        }

        // overload
        public XElement GetDefault(string elementName)
        {
            XElement query = new XElement(elementName);
            return GetDefault(query);
        }

        // overload
        public XElement GetDefault(string elementName, XElement schema)
        {
            XElement query = new XElement(elementName);
            return GetDefault(query, schema);
        }

        // overload
        public XElement GetDefault(string elementName, string schemaName)
        {
            XElement query = new XElement(elementName);
            return GetDefault(query, schemaName);
        }

        // overload
        public XElement GetDefault(ElementQuery query)
        {
            if (query.Schema != null)
            {
                return GetDefault(query.Query, query.Schema);
            }
            if (string.IsNullOrWhiteSpace(query.SchemaName))
            {
                return GetDefault(query.Query, query.SchemaName);
            }
            return GetDefault(query.Query);
        }

        // Element"(Where")
        // overload
        public int GetCount(XElement query)
        {
            return GetCount(query, PrimarySchema);
        }

        public int GetCount(XElement query, XElement schema)
        {
            return Reader.GetCount(query, schema);
        }

        // overload
        public int GetCount(XElement query, string schemaName)
        {
            XElement schema = GetSchema(schemaName);
            return GetCount(query, schema);
        }

        // overload
        public int GetCount(string elementName)
        {
            XElement query = new XElement(elementName);
            query.Add(new XElement("Where"));
            return GetCount(query);
        }

        // overload
        public int GetCount(string elementName, XElement schema)
        {
            XElement query = new XElement(elementName);
            query.Add(new XElement("Where"));
            return GetCount(query, schema);
        }

        // overload
        public int GetCount(string elementName, string schemaName)
        {
            XElement query = new XElement(elementName);
            query.Add(new XElement("Where"));
            return GetCount(query, schemaName);
        }

        // overload
        public int GetCount(ElementQuery query)
        {
            if (query.Schema != null)
            {
                return GetCount(query.Query, query.Schema);
            }
            if (string.IsNullOrWhiteSpace(query.SchemaName))
            {
                return GetCount(query.Query, query.SchemaName);
            }
            return GetCount(query.Query);
        }

        // List, Page
        // overload
        public XElement GetSet(XElement query)
        {
            return GetSet(query, PrimarySchema);
        }

        public XElement GetSet(XElement query, XElement schema)
        {
            return Reader.GetSet(query, schema);
        }

        internal IEnumerable<T> GetSet<T>(XElement query, XElement schema, IFastGetter<T> fastGetter)
        {
            return Reader.GetSet(query, schema, fastGetter);
        }

        // overload
        public XElement GetSet(XElement query, string schemaName)
        {
            XElement schema = GetSchema(schemaName);
            return GetSet(query, schema);
        }

        // overload
        public XElement GetSet(string elementName)
        {
            XElement query = new XElement(elementName);
            return GetSet(query);
        }

        // overload
        public XElement GetSet(string elementName, XElement schema)
        {
            XElement query = new XElement(elementName);
            return GetSet(query, schema);
        }

        // overload
        public XElement GetSet(string elementName, string schemaName)
        {
            XElement query = new XElement(elementName);
            return GetSet(query, schemaName);
        }

        // overload
        public XElement GetSet(ElementQuery query)
        {
            if (query.Schema != null)
            {
                return GetSet(query.Query, query.Schema);
            }
            if (string.IsNullOrWhiteSpace(query.SchemaName))
            {
                return GetSet(query.Query, query.SchemaName);
            }
            return GetSet(query.Query);
        }

        // Specification Pattern, Linq
        public ElementSet GetElementSet(string elementName)
        {
            return new ElementSet(this, elementName);
        }

        public ElementSet GetElementSet(string elementName, XElement schema)
        {
            return new ElementSet(this, elementName, schema);
        }

        public ElementSet GetElementSet(string elementName, string schemaName)
        {
            return new ElementSet(this, elementName, schemaName);
        }


    }
}
