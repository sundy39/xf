using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class BizSpecifiedConfigGetter : XmlSpecifiedConfigGetter
    {
        public BizSpecifiedConfigGetter(ElementContext elementContext, XElement specifiedConfig)
            : base(elementContext, specifiedConfig)
        {
        }

        public override XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return base.Get(nameValuePairs);
        }


    }
}
