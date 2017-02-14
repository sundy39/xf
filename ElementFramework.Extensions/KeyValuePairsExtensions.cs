using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Extensions
{
    public static class KeyValuePairsExtensions
    {
        public static string GetValue(this IEnumerable<KeyValuePair<string, string>> nameValuePairs, string key)
        {
            string value = null;
            if (nameValuePairs.Any(p => p.Key == key))
            {
                value = nameValuePairs.First(p => p.Key == key).Value;
            }
            return value;
        }

        public static XElement GetSchema(this IEnumerable<KeyValuePair<string, string>> nameValuePairs, XElement schema)
        {
            string primes = GetValue(nameValuePairs, "primes");
            if (string.IsNullOrWhiteSpace(primes)) return schema;

            //
            XElement modifying = new XElement("Config");
            string[] names = primes.Split(',');
            for (int i = 0; i < names.Length; i++)
            {
                XElement schemaElement = schema.Elements().FirstOrDefault(x => x.Attribute("Name") != null && x.Attribute("Name").Value == names[i].Trim());
                if (schemaElement == null) continue;
                if (schemaElement.Element("Prime") == null)
                {
                    XElement modifyingElement = new XElement(schemaElement);
                    modifyingElement.Add(new XElement("Prime"));
                    modifying.Add(modifyingElement);
                }
            }
            schema.Modify(modifying);
            return schema;
        }
    }
}
