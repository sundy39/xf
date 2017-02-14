using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace XData.Data.Query
{
    public class ElementQuery : IOrderedQueryable<XElement>, IQueryable<XElement>, IEnumerable<XElement>, IOrderedQueryable, IQueryable, IEnumerable
    {
        private readonly XElement _innerQuery;
        internal protected XElement InnerQuery
        {
            get { return _innerQuery; }
        }

        public XElement Query
        {
            get { return new XElement(InnerQuery); }
        }

        protected readonly XElement Element = new XElement("Element");

        public virtual IEnumerator<XElement> GetEnumerator()
        {
            return Element.Elements().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Type ElementType
        {
            get { return Element.Elements().AsQueryable().ElementType; }
        }

        public Expression Expression
        {
            get { return Element.Elements().AsQueryable().Expression; }
        }

        public IQueryProvider Provider
        {
            get { return Element.Elements().AsQueryable().Provider; }
        }

        public XElement Schema { get; private set; }

        public string SchemaName { get; private set; }

        public ElementQuery(string elementName)
        {
            _innerQuery = new XElement(elementName);
        }

        public ElementQuery(string elementName, XElement schema)
        {
            Schema = schema;
            _innerQuery = new XElement(elementName);
        }

        public ElementQuery(string elementName, string schemaName)
        {
            SchemaName = schemaName;
            _innerQuery = new XElement(elementName);
        }


    }
}
