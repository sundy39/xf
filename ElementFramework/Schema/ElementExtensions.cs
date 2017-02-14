using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Xml.Linq
{
    public static class ElementExtensions
    {
        public static IEnumerable<XElement> Filter(this IEnumerable<XElement> elements, XElement value)
        {
            IEnumerable<XElement> elmts = elements;
            foreach (XElement keyField in value.Elements())
            {
                elmts = elmts.Where(p => p.Element(keyField.Name).Value == keyField.Value);
            }
            return elmts;
        }

    }
}
