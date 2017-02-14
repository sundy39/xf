using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Objects
{
    [Serializable]
    public class ConcurrencyCheckException : Exception, IElementException
    {
        public XElement Node { get; private set; }
        public XElement OrigNode { get; private set; }
        public string XPath { get; private set; }
        public XElement Element { get; private set; }
        public XElement Schema { get; private set; }

        public ConcurrencyCheckException(XElement node, XElement origNode, string xPath, XElement element, XElement schema)
            : this(Messages.ConcurrencyCheck_Failed, node, origNode, xPath, element, schema)
        {
        }

        public ConcurrencyCheckException(string message, XElement node, XElement origNode, string xPath, XElement element, XElement schema)
            : base(message)
        {
            Node = new XElement(node);
            Node.RemoveNodes();
            OrigNode = new XElement(origNode);
            OrigNode.RemoveNodes();
            XPath = xPath;
            Element = new XElement(element);
            Schema = new XElement(schema);
        }

        public XElement ToElement()
        {
            XElement error = new XElement("Error");
            error.Add(new XElement("ExceptionMessage", Message));
            error.Add(new XElement("ExceptionType", this.GetType().FullName));

            //
            XElement node = new XElement(Node);
            node.RemoveNodes();
            error.Add(node);
            return error;
        }


    }
}
