using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public class Select
    {
        private string _string;
        private XElement _element;

        public Select(string select)
        {
            _string = select;
            _element = new XElement("Select");
            string[] fields = select.Split(',');
            foreach (string field in fields)
            {
                _element.Add(new XElement(field.Trim()));
            }
        }

        public Select(XElement select)
        {
            _element = select;
            _string = string.Join(",", _element.Elements().Select(p => p.Name.LocalName));
        }

        public XElement ToElement()
        {
            return _element;
        }

        public override string ToString()
        {
            return _string;
        }
    }
}
