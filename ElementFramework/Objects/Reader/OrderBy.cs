using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public class OrderBy
    {
        private List<XElement> _elements = new List<XElement>();

        public OrderBy(string orderby)
        {
            AntiSqlInjection(orderby);

            string[] items = orderby.Split(',');
            items[0] = items[0].Trim();
            string[] first_field_direction = items[0].Split(new char[] { (char)32 }, StringSplitOptions.RemoveEmptyEntries);
            string firstField = first_field_direction[0].Trim();
            bool firstIsDesc = false;
            if (first_field_direction.Length == 2)
            {
                firstIsDesc = first_field_direction[1].Trim().ToLower() == "desc";
            }

            XElement firstElement = firstIsDesc ? new XElement("OrderByDescending") : new XElement("OrderBy");
            firstElement.Add(new XElement(firstField));
            _elements.Add(firstElement);

            for (int i = 1; i < items.Length; i++)
            {
                string[] field_direction = items[i].Trim().Split(new char[] { (char)32 }, StringSplitOptions.RemoveEmptyEntries);
                string field = field_direction[0].Trim();
                bool isDesc = false;
                if (field_direction.Length == 2)
                {
                    isDesc = field_direction[1].Trim().ToLower() == "desc";
                }

                XElement element = isDesc ? new XElement("ThenByDescending") : new XElement("ThenBy");
                element.Add(new XElement(field.Trim()));
                _elements.Add(element);
            }
        }

        private void AntiSqlInjection(string orderby)
        {
            string[] items = orderby.Split(',');
            foreach (string item in items)
            {
                string field = item.Trim();
                if (field.EndsWith("asc", StringComparison.OrdinalIgnoreCase))
                {
                    field = field.Substring(0, field.Length - 3);
                    if (field[field.Length - 1] != (char)32) throw new SqlInjectionException();
                    field = field.TrimEnd();
                }
                else if (field.EndsWith("desc", StringComparison.OrdinalIgnoreCase))
                {
                    field = field.Substring(0, field.Length - 4);
                    if (field[field.Length - 1] != (char)32) throw new SqlInjectionException();
                    field = field.TrimEnd();
                }

                try
                {
                    XElement element = new XElement(field); 
                }
                catch
                {
                    throw new SqlInjectionException();
                }
            }
        }

        public OrderBy(params XElement[] orderby)
        {
            _elements = orderby.ToList();
        }

        public XElement[] ToElements()
        {
            return _elements.ToArray();
        }

        public override string ToString()
        {
            return ToString("ASC", "DESC");
        }

        public string ToString(string asc, string desc)
        {
            List<string> orderBy = new List<string>();
            foreach (XElement element in _elements)
            {
                string field = element.Elements().First().Name.LocalName;
                string direction = element.Name.LocalName.EndsWith("Descending") ? desc : asc;
                string item = string.Format("{0} {1}", field, direction);
                orderBy.Add(item);
            }
            return string.Join(",", orderBy);
        }
    }
}
