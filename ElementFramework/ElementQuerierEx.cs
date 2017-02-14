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
    public class ElementQuerierEx : ElementQuerier
    {
        public ElementQuerierEx(ElementContext elementContext)
            : base(elementContext)
        {
        }

        public override XElement GetDefault(string elementName, string select, XElement schema)
        {
            XElement newSchema = new XElement(schema);
            newSchema.SetAttributeValue("Ex", true.ToString());
            return base.GetDefault(elementName, select, newSchema);
        }

        public override XElement Find(string elementName, string[] key, string select, XElement schema)
        {
            XElement newSchema = new XElement(schema);
            newSchema.SetAttributeValue("Ex", true.ToString());
            return base.Find(elementName, key, select, newSchema);
        }

        public override XElement Find(string elementName, string[] key, string select, IEnumerable<Expand> expands, XElement schema)
        {
            XElement newSchema = new XElement(schema);
            newSchema.SetAttributeValue("Ex", true.ToString());
            return base.Find(elementName, key, select, expands, newSchema);
        }

        public override XElement GetSet(string elementName, string select, string filter, string orderby, XElement schema)
        {
            XElement newSchema = new XElement(schema);
            newSchema.SetAttributeValue("Ex", true.ToString());
            return base.GetSet(elementName, select, filter, orderby, newSchema);
        }

        public override XElement GetSet(string elementName, string select, string filter, string orderBy, IEnumerable<Expand> expands, XElement schema)
        {
            XElement newSchema = new XElement(schema);
            newSchema.SetAttributeValue("Ex", true.ToString());
            return base.GetSet(elementName, select, filter, orderBy, expands, newSchema);
        }

        public override XElement GetPage(string elementName, string select, string filter, string orderBy, int skip, int take, XElement schema)
        {
            XElement newSchema = new XElement(schema);
            newSchema.SetAttributeValue("Ex", true.ToString());
            return base.GetPage(elementName, select, filter, orderBy, skip, take, newSchema);
        }

        public override XElement GetPage(string elementName, string select, string filter, string orderBy, int skip, int take, IEnumerable<Expand> expands, XElement schema)
        {
            XElement newSchema = new XElement(schema);
            newSchema.SetAttributeValue("Ex", true.ToString());
            return base.GetPage(elementName, select, filter, orderBy, skip, take, expands, newSchema);
        }

        internal virtual IEnumerable<T> GetSet<T>(string elementName, string select, string filter, string orderby, XElement schema, IFastGetter<T> fastGetter)
        {
            XElement referenceSchema;
            XElement query = GetQuery(elementName, select, filter, orderby, schema, out referenceSchema);
            XElement modifying = new XElement("Config");
            modifying.Add(referenceSchema);

            XElement newSchema = new XElement(schema);
            newSchema.Modify(modifying);

            return ElementContext.GetSet(query, newSchema, fastGetter);
        }

        internal virtual IEnumerable<T> GetPage<T>(string elementName, string select, string filter, string orderby, int skip, int take, XElement schema, IFastGetter<T> fastGetter)
        {
            XElement referenceSchema;
            XElement query = GetQuery(elementName, select, filter, orderby, skip, take, schema, out referenceSchema);
            XElement modifying = new XElement("Config");
            modifying.Add(referenceSchema);

            XElement newSchema = new XElement(schema);
            newSchema.Modify(modifying);

            return ElementContext.GetSet(query, newSchema, fastGetter);
        }


    }
}
