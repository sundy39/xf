﻿using System;
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
    public class CurrentUserIdentityGetterFactory
    {
        public ICurrentUserIdentityGetter Create()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            CurrentUserIdentityGetterConfigurationElement configurationElement = configurationSection.CurrentUserIdentityGetter;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            XElement config = new XElement("currentUserIdentityGetter");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            return objectCreator.CreateInstance() as ICurrentUserIdentityGetter;
        }


    }
}
