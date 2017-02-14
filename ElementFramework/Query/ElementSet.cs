using System.Collections.Generic;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Query
{
    public class ElementSet : ElementQuery
    {
        protected ElementContext ElementContext;

        internal protected ElementSet(ElementContext elementContext, string elementName)
            : base(elementName)
        {
            ElementContext = elementContext;
        }

        internal protected ElementSet(ElementContext elementContext, string elementName, XElement schema)
            : base(elementName, schema)
        {
            ElementContext = elementContext;
        }

        internal protected ElementSet(ElementContext elementContext, string elementName, string schemaName)
            : base(elementName, schemaName)
        {
            ElementContext = elementContext;
        }

        public override IEnumerator<XElement> GetEnumerator()
        {
            XElement result;
            if (Schema != null)
            {
                result = ElementContext.GetSet(Query, Schema);
            }
            else if (!string.IsNullOrWhiteSpace(SchemaName))
            {
                result = ElementContext.GetSet(Query, SchemaName);
            }
            else
            {
                result = ElementContext.GetSet(Query);
            }
            return result.Elements().GetEnumerator();
        }
    }
}
