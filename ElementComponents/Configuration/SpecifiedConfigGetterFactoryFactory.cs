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
    internal class SpecifiedConfigGetterFactoryFactory
    {
        public SpecifiedConfigGetterFactory Create()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            SpecifiedConfigGetterFactoryConfigurationElement configurationElement = configurationSection.SpecifiedConfigGetterFactory as SpecifiedConfigGetterFactoryConfigurationElement;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            XElement config = new XElement("specifiedConfigGetterFactory");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            SpecifiedConfigGetterFactory factory = objectCreator.CreateInstance() as SpecifiedConfigGetterFactory;
            return factory;
        }
    }
}
