using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Components;
using XData.Data.Element;
using XData.Data.Objects;

namespace XData.Data.Configuration
{
    internal class DataSourceFactoryFactory
    {
        public DataSourceFactory Create()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            DataSourceFactoryConfigurationElement configurationElement = configurationSection.DataSourceFactory as DataSourceFactoryConfigurationElement;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            if (type == "XData.Data.Components.DataSourceFactory,ElementComponents")
            {
                return new DataSourceFactory();
            }
            else if (type == "XData.Data.Components.ConfigurableDataSourceFactory,ElementComponents")
            {
                return new ConfigurableDataSourceFactory();
            }

            XElement config = new XElement("dataSourceFactory");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            DataSourceFactory factory = objectCreator.CreateInstance() as DataSourceFactory;
            return factory;
        }
    }
}
