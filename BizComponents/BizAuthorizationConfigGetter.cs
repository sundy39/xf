using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class BizAuthorizationConfigGetter : XmlAuthorizationConfigGetter
    {
        public BizAuthorizationConfigGetter(ElementContext elementContext, XElement authorizationConfig)
            : base(elementContext, authorizationConfig)
        {
        }

        public override XElement Get(string route)
        {
            return base.Get(route);
        }
    }
}
