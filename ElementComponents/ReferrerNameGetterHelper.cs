using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Extensions;

namespace XData.Data.Components
{
    internal static class ReferrerNameGetterHelper
    {
        internal static XElement InitializeContainer(XElement original)
        {
            XElement result = new XElement(original.Name);
            foreach (XElement config in original.Elements())
            {
                XAttribute attr = config.Attribute("referrer");
                if (attr == null) continue;

                string referrer = attr.Value;
                string[] ss = referrer.Split('|');
                for (int i = 0; i < ss.Length; i++)
                {
                    XElement cfg = new XElement(config);
                    cfg.SetAttributeValue("referrer", ss[i]);
                    result.Add(cfg);
                }
            }
            return result;
        }

        internal static XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs, XElement container)
        {
            string referrer = nameValuePairs.GetValue("referrer");
            if (string.IsNullOrWhiteSpace(referrer)) return null;

            IEnumerable<XElement> configs;
            string name = nameValuePairs.GetValue("name");
            if (string.IsNullOrWhiteSpace(name))
            {
                configs = container.Elements()
                    .Where(x => x.Attribute("referrer") != null && x.Attribute("referrer").Value == referrer &&
                                x.Attribute("name") == null);
            }
            else
            {
                configs = container.Elements()
                        .Where(x => x.Attribute("referrer") != null && x.Attribute("referrer").Value == referrer &&
                                    x.Attribute("name") != null && x.Attribute("name").Value == name);
            }
            int count = configs.Count();
            if (count == 1) return configs.First();
            if (count > 1) throw new Exception(string.Join(",", referrer, name));

            XElement result = MatchConfig(referrer, name, container);
            if (result != null) return result;

            // referrer: *
            configs = container.Elements()
                   .Where(x => x.Attribute("referrer") != null && x.Attribute("referrer").Value == "*" &&
                               x.Attribute("name") != null && x.Attribute("name").Value == name);
            count = configs.Count();
            if (count == 1) return configs.First();
            if (count > 1) throw new Exception(string.Join(",", referrer, name));
            return null;
        }

        private static XElement MatchConfig(string referrer, string name, XElement container)
        {
            IEnumerable<XElement> configs;
            if (string.IsNullOrWhiteSpace(name))
            {
                configs = container.Elements();
            }
            else
            {
                configs = container.Elements().Where(x => x.Attribute("name") != null && x.Attribute("name").Value == name);
            }

            string[] rAarray = referrer.Split('/');
            List<XElement> result = new List<XElement>();
            foreach (XElement config in configs)
            {
                XAttribute attr = config.Attribute("referrer");
                if (attr == null) continue;

                string[] aAarray = attr.Value.Split('/');
                if (rAarray.Length != aAarray.Length) continue;

                bool isMatched = true;
                for (int i = 0; i < aAarray.Length; i++)
                {
                    string s = aAarray[i];
                    if (s.StartsWith("{") && s.EndsWith("}")) continue;

                    if (rAarray[i] != s)
                    {
                        isMatched = false;
                        break;
                    }
                }

                if (isMatched)
                {
                    result.Add(config);
                }
            }
            if (result.Count == 0) return null;
            if (result.Count > 1) throw new Exception(string.Join(",", referrer, name));
            return result.First();
        }


    }
}
