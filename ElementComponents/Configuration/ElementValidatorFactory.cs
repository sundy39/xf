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
    internal class ElementValidatorFactory
    {
        public ElementValidator Create(ElementContext elementContext)
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            ElementValidatorConfigurationElement configurationElement = configurationSection.ElementValidator as ElementValidatorConfigurationElement;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            if (type == "XData.Data.Components.ElementValidator,ElementComponents")
            {
                return new ElementValidator() { ElementContext = elementContext };
            }

            XElement config = new XElement("elementValidator");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            ElementValidator validator = objectCreator.CreateInstance() as ElementValidator;

            validator.ElementContext = elementContext;
            return validator;
        }


    }
}
