using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class BizDataSourceConfigGetter : XmlDataSourceConfigGetter
    {
        public BizDataSourceConfigGetter(ElementContext elementContext, XElement dataSourceConfig)
            : base(elementContext, dataSourceConfig)
        {
        }

        public override XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return base.Get(nameValuePairs);
        }


    }
}
