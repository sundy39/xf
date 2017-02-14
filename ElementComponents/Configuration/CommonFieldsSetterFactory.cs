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
    internal class CommonFieldsSetterFactory
    {
        public CommonFieldsSetter Create()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            CommonFieldsSetterConfigurationElement configurationElement = configurationSection.CommonFieldsSetter as CommonFieldsSetterConfigurationElement;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            if (type == "XData.Data.Components.CommonFieldsSetter,ElementComponents")
            {
                return new CommonFieldsSetter();
            }

            XElement config = new XElement("commonFieldsSetter");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            return objectCreator.CreateInstance() as CommonFieldsSetter;
        }


    }
}
