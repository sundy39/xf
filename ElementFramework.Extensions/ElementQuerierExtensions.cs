using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public static class ElementQuerierExtensions
    {
        // overload
        public static bool IsUnique(this ElementQuerier elementQuerier, string elementName, string filter, XElement schema)
        {
            return IsUnique(elementQuerier, elementName, filter, new string[0], schema);
        }

        // overload
        public static bool IsUnique(this ElementQuerier elementQuerier, string elementName, string filter, string excludedKey, XElement schema)
        {
            string[] excluded = null;
            if (!string.IsNullOrWhiteSpace(excludedKey))
            {
                excluded = new string[] { excludedKey };
            }
            return IsUnique(elementQuerier, elementName, filter, excluded, schema);
        }

        public static bool IsUnique(this ElementQuerier elementQuerier, string elementName, string filter, string[] excludedKey, XElement schema)
        {
            XElement result = elementQuerier.GetSet(elementName, null, filter, null);
            int count = result.Elements().Count();
            if (count == 0) return true;
            if (count == 1)
            {
                XElement element = result.Elements().First();
                if (excludedKey.Length == 0) return false;
                XElement key = schema.GetElementSchema(elementName).GetKeySchema().ExtractKey(element);
                var keyVals = key.Elements().Select(x => x.Value).ToArray();
                for (int i = 0; i < keyVals.Length; i++)
                {
                    if (excludedKey[i] != keyVals[i]) return false;
                }
                return true;
            }
            return false;
        }


    }
}
