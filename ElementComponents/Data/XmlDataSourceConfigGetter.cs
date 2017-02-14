using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class XmlDataSourceConfigGetter : DataSourceConfigGetter
    {
        protected XElement DataSourceConfig;

        public XmlDataSourceConfigGetter(ElementContext elementContext, XElement dataSourceConfig)
            : base(elementContext)
        {
            DataSourceConfig = ReferrerNameGetterHelper.InitializeContainer(dataSourceConfig);
        }

        public override XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            try
            {
                return ReferrerNameGetterHelper.Get(nameValuePairs, DataSourceConfig);
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(Exception))
                {
                    throw new Exception(string.Format("Multiple dataSource configs were found that match {0}", e.Message));
                }
                throw e;
            }
        }


    }
}
