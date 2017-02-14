using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class BizDataSource : ConfigurableDataSource
    {
        public BizDataSource(ElementContext elementContext, SpecifiedConfigGetter specifiedConfigGetter, DataSourceConfigGetter dataSourceConfigGetter)
            : base(elementContext, specifiedConfigGetter, dataSourceConfigGetter)
        {
        }

        public override XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            return base.Get(nameValuePairs, accept);
        }

        public override XElement Create(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return base.Create(value, nameValuePairs);
        }

        public override XElement Delete(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return base.Delete(nameValuePairs);
        }

        public override XElement Update(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return base.Update(value, nameValuePairs);
        }


    }
}
