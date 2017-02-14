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
    internal class AuthorizationConfigGetterFactoryFactory
    {
        public AuthorizationConfigGetterFactory Create()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            AuthorizationConfigGetterFactoryConfigurationElement configurationElement = configurationSection.AuthorizationConfigGetterFactory as AuthorizationConfigGetterFactoryConfigurationElement;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            XElement config = new XElement("authorizationConfigGetterFactory");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            AuthorizationConfigGetterFactory factory = objectCreator.CreateInstance() as AuthorizationConfigGetterFactory;
            return factory;
        }
    }
}
