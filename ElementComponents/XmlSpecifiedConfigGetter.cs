using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;
using XData.Data.Extensions;

namespace XData.Data.Components
{
    public class XmlSpecifiedConfigGetter : SpecifiedConfigGetter
    {
        protected XElement SpecifiedConfig;

        public XmlSpecifiedConfigGetter(ElementContext elementContext, XElement specifiedConfig)
            : base(elementContext)
        {
            SpecifiedConfig = ReferrerNameGetterHelper.InitializeContainer(specifiedConfig);
        }

        public override XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            try
            {
                return ReferrerNameGetterHelper.Get(nameValuePairs, SpecifiedConfig);
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(Exception))
                {
                    throw new Exception(string.Format("Multiple specified configs were found that match {0}", e.Message));
                }
                throw e;
            }
        }


    }
}
