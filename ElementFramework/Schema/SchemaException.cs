using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    [Serializable]
    public class SchemaException : Exception, IElementException
    {
        public XElement Schema { get; private set; }
        public XElement Content { get; private set; }

        public SchemaException(string message, XElement schema)
            : base(message)
        {
            Schema = new XElement(schema);
        }

        public SchemaException(string message, XElement element, XElement schema)
            : this(message, schema)
        {
            Content = element;
        }

        public XElement ToElement()
        {
            XElement error = new XElement("Error");
            error.Add(new XElement("ExceptionMessage", Message));
            error.Add(new XElement("ExceptionType", this.GetType().FullName));

            //
            XElement schema = new XElement(Schema);
            schema.RemoveNodes();           
            if (Content != null)
            {
                XElement content = new XElement(Content);
                content.RemoveNodes();
                schema.Add(new XElement("Content", content));
            }
            error.Add(schema);
            return error;
        }


    }
}
