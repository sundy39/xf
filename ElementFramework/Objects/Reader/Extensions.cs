using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    internal static class Extensions
    {
        public static void FillFilter(this XElement filter, string value, string elementName, XElement schema)
        {
            Debug.Assert(filter.Name.LocalName == "Filter");
            filter.Add(new XElement("Value", value));
            XElement fields = new XElement("Fields");
            fields.Add(ExtractFields(value, elementName, schema));
            filter.Add(fields);
        }

        private static List<XElement> ExtractFields(string value, string elementName, XElement schema)
        {
            string blank = new string((char)32, 1);
            string filter = value;

            filter = filter.Replace("''", blank);

            string pattern = @"'.+?'";
            filter = Regex.Replace(filter, pattern, new MatchEvaluator(m =>
            {
                return blank;
            }));

            List<string> list = new List<string>();
            pattern = @"\b[A-Za-z_]\w*\.[A-Za-z_]\w*\b";
            filter = Regex.Replace(filter, pattern, new MatchEvaluator(m =>
            {
                list.Add(m.Value);
                return blank;
            }));

            XElement elementSchema = schema.GetElementSchema(elementName);

            var matches = Regex.Matches(filter, @"\b[A-Za-z_]\w*\b");
            foreach (Match match in matches)
            {
                if (elementSchema.Element(match.Value) != null)
                {
                    list.Add(match.Value);
                }
            }

            //
            List<XElement> fields = new List<XElement>();
            foreach (string field in list.Distinct())
            {
                fields.Add(new XElement(field));
            }
            return fields;
        }
    }
}
