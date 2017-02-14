using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public class ElementQuerier<T>
    {
        protected ElementContext ElementContext;
        protected Converter Converter;
        protected IFastGetter<T> FastGetter;
        protected ElementQuerierEx ElementQuerierEx;

        public ElementQuerier(ElementContext elementContext, Converter converter)
        {
            ElementContext = elementContext;
            Converter = converter;
            if (converter is IFastGetter<T>)
            {
                FastGetter = converter as IFastGetter<T>;
            }
            ElementQuerierEx = new ElementQuerierEx(elementContext);
        }

        public ElementQuerier(ElementContext elementContext, Converter converter, IFastGetter<T> fastGetter)
        {
            ElementContext = elementContext;
            Converter = converter;
            FastGetter = fastGetter;
            ElementQuerierEx = new ElementQuerierEx(elementContext);
        }

        public virtual DateTime GetNow()
        {
            return ElementContext.GetNow();
        }

        public virtual DateTime GetUtcNow()
        {
            return ElementContext.GetUtcNow();
        }

        // overload
        public T GetDefault(string elementName)
        {
            return GetDefault(elementName, null, ElementContext.PrimarySchema);
        }

        // overload
        public T GetDefault(string elementName, string select)
        {
            return GetDefault(elementName, select, ElementContext.PrimarySchema);
        }

        // overload
        public T GetDefault(string elementName, string select, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetDefault(elementName, select, schema);
        }

        public virtual T GetDefault(string elementName, string select, XElement schema)
        {
            XElement result = ElementQuerierEx.GetDefault(elementName, select, schema);
            return (T)Converter.ToObject(result);
        }

        // overload
        public int GetCount(string setName)
        {
            return GetCount(setName, null, ElementContext.PrimarySchema);
        }

        // overload
        public int GetCount(string setName, string filter)
        {
            return GetCount(setName, filter, ElementContext.PrimarySchema);
        }

        // overload
        public int GetCount(string elementName, string filter, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetCount(elementName, filter, schema);
        }

        public virtual int GetCount(string setName, string filter, XElement schema)
        {
            string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;
            int count = ElementQuerierEx.GetCount(elementName, filter, schema);
            return count;
        }

        // overload
        public T Find(string elementName, string[] key)
        {
            return Find(elementName, key, string.Empty);
        }

        // overload
        public T Find(string elementName, string[] key, string select)
        {
            return Find(elementName, key, select, ElementContext.PrimarySchema);
        }

        // overload
        public T Find(string elementName, string[] key, string select, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return Find(elementName, key, select, schema);
        }

        public virtual T Find(string elementName, string[] key, string select, XElement schema)
        {
            XElement result = ElementQuerierEx.Find(elementName, key, select, schema);
            if (result == null) return default(T);
            return (T)Converter.ToObject(result);
        }

        // overload
        public T Find(string elementName, string[] key, IEnumerable<Expand> expands)
        {
            return Find(elementName, key, null, expands, ElementContext.PrimarySchema);
        }

        // overload
        public T Find(string elementName, string[] key, string select, IEnumerable<Expand> expands)
        {
            return Find(elementName, key, select, expands, ElementContext.PrimarySchema);
        }

        // overload
        public T Find(string elementName, string[] key, string select, IEnumerable<Expand> expands, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return Find(elementName, key, select, expands, schema);
        }

        public virtual T Find(string elementName, string[] key, string select, IEnumerable<Expand> expands, XElement schema)
        {
            XElement result = ElementQuerierEx.Find(elementName, key, select, expands, schema);
            if (result == null) return default(T);
            return (T)Converter.ToObject(result);
        }

        // overload
        public IEnumerable<T> GetSet(string elementName, string select, string filter, string orderBy)
        {
            return GetSet(elementName, select, filter, orderBy, ElementContext.PrimarySchema);
        }

        // overload
        public IEnumerable<T> GetSet(string elementName, string select, string filter, string orderby, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetSet(elementName, select, filter, orderby, schema);
        }

        public virtual IEnumerable<T> GetSet(string elementName, string select, string filter, string orderby, XElement schema)
        {
            return ElementQuerierEx.GetSet(elementName, select, filter, orderby, schema, FastGetter);
        }

        // overload
        public IEnumerable<T> GetSet(string elementName, string select, string filter, string orderBy, IEnumerable<Expand> expands)
        {
            return GetSet(elementName, select, filter, orderBy, expands, ElementContext.PrimarySchema);
        }

        // overload
        public IEnumerable<T> GetSet(string elementName, string select, string filter, string orderBy, IEnumerable<Expand> expands, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetSet(elementName, select, filter, orderBy, expands, schema);
        }

        public virtual IEnumerable<T> GetSet(string elementName, string select, string filter, string orderBy, IEnumerable<Expand> expands, XElement schema)
        {
            XElement result = ElementQuerierEx.GetSet(elementName, select, filter, orderBy, expands, schema);
            return (IEnumerable<T>)Converter.ToObject(result);
        }

        // overload
        public IEnumerable<T> GetPage(string elementName, string select, string filter, string orderBy, int skip, int take)
        {
            return GetPage(elementName, select, filter, orderBy, skip, take, ElementContext.PrimarySchema);
        }

        // overload
        public IEnumerable<T> GetPage(string elementName, string select, string filter, string orderby, int skip, int take, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetPage(elementName, select, filter, orderby, skip, take, schema);
        }

        public virtual IEnumerable<T> GetPage(string elementName, string select, string filter, string orderby, int skip, int take, XElement schema)
        {
            return ElementQuerierEx.GetPage(elementName, select, filter, orderby, skip, take, schema, FastGetter);
        }

        // overload
        public IEnumerable<T> GetPage(string elementName, string select, string filter, string orderby, int skip, int take, IEnumerable<Expand> expands)
        {
            return GetPage(elementName, select, filter, orderby, skip, take, expands, ElementContext.PrimarySchema);
        }

        // overload
        public IEnumerable<T> GetPage(string elementName, string select, string filter, string orderby, int skip, int take, IEnumerable<Expand> expands, string schemaName)
        {
            XElement schema = ElementContext.GetSchema(schemaName);
            return GetPage(elementName, select, filter, orderby, skip, take, expands, schema);
        }

        public virtual IEnumerable<T> GetPage(string elementName, string select, string filter, string orderby, int skip, int take, IEnumerable<Expand> expands, XElement schema)
        {
            XElement result = ElementQuerierEx.GetPage(elementName, select, filter, orderby, skip, take, expands, schema);
            return (IEnumerable<T>)Converter.ToObject(result);
        }


    }
}
