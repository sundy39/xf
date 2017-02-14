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
    internal class ElementContextFactoryFactory
    {
        public ElementContextFactory Create()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            ElementContextFactoryConfigurationElement configurationElement = configurationSection.ElementContextFactory as ElementContextFactoryConfigurationElement;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            if (type == "XData.Data.Components.ElementContextFactory,ElementComponents")
            {
                return new ElementContextFactory();
            }

            XElement config = new XElement("elementContextFactory");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            ElementContextFactory factory = objectCreator.CreateInstance() as ElementContextFactory;
            return factory;
        }


    }
}
