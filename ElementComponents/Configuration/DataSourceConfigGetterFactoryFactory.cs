using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Components;
using XData.Data.Objects;

namespace XData.Data.Configuration
{
    internal class DataSourceConfigGetterFactoryFactory
    {
        public DataSourceConfigGetterFactory Create()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            DataSourceConfigGetterFactoryConfigurationElement configurationElement = configurationSection.DataSourceConfigGetterFactory as DataSourceConfigGetterFactoryConfigurationElement;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            XElement config = new XElement("dataSourceConfigGetterFactory");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            DataSourceConfigGetterFactory factory = objectCreator.CreateInstance() as DataSourceConfigGetterFactory;
            return factory;
        }
    }
}
