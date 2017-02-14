using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class BizDataSourceConfigGetterFactory : DataSourceConfigGetterFactory
    {
        protected static XElement DataSourceConfig = null;

        public override DataSourceConfigGetter Create(ElementContext elementContext)
        {
            if (DataSourceConfig == null)
            {
                string fileName = ConfigurationManager.AppSettings["BizDataSourceConfigGetterFactory.FileName"];
                if (!File.Exists(fileName))
                {
                    fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                }
                DataSourceConfig = XElement.Load(fileName);
            }
            return new BizDataSourceConfigGetter(elementContext, DataSourceConfig);
        }
    }
}
