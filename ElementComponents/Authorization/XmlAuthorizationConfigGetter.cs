using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class XmlAuthorizationConfigGetter : AuthorizationConfigGetter
    {
        protected XElement AuthorizationConfig;

        public XmlAuthorizationConfigGetter(ElementContext elementContext, XElement authorizationConfig)
            : base(elementContext)
        {
            AuthorizationConfig = InitializeContainer(authorizationConfig);
        }

        private static XElement InitializeContainer(XElement original)
        {
            XElement result = new XElement(original.Name);
            foreach (XElement config in original.Elements())
            {
                XAttribute attr = config.Attribute("Route");
                if (attr == null) continue;

                string referrer = attr.Value;
                string[] ss = referrer.Split('|');
                for (int i = 0; i < ss.Length; i++)
                {
                    XElement cfg = new XElement(config);
                    cfg.SetAttributeValue("Route", ss[i]);
                    result.Add(cfg);
                }
            }
            return result;
        }

        public override XElement Get(string route)
        {
            List<string> list = new List<string>();
            string[] ss = route.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length == 0)
            {
                list.Add("/");
            }
            else
            {
                string s = "/" + ss[0];
                list.Add(s);
                for (int i = 1; i < ss.Length; i++)
                {
                    s = s + "/" + ss[i];
                    list.Add(s);
                }
                list.Reverse();
            }

            foreach (string path in list)
            {
                IEnumerable<XElement> configs = AuthorizationConfig.Elements().Where(x =>
                    x.Attribute("Route") != null && x.Attribute("Route").Value.TrimEnd('/') == path);
                int count = configs.Count();
                if (count == 1) return configs.First();
                if (count > 1) throw new Exception(string.Format("Multiple authorization configs were found that match {0}", route));
            }

            return null;
        }


    }
}
