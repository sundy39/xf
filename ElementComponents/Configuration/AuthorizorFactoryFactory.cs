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
    internal class AuthorizorFactoryFactory
    {
        public AuthorizorFactory Create()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            AuthorizorFactoryConfigurationElement configurationElement = configurationSection.AuthorizorFactory as AuthorizorFactoryConfigurationElement;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            if (type == "XData.Data.Components.AuthorizorFactory,ElementComponents")
            {
                return new AuthorizorFactory();
            }

            XElement config = new XElement("authorizorFactory");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            AuthorizorFactory factory = objectCreator.CreateInstance() as AuthorizorFactory;
            return factory;
        }
    }
}
