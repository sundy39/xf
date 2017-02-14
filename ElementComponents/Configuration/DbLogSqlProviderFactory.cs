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
    internal class DbLogSqlProviderFactory
    {
        public DbLogSqlProvider Create(ElementContext elementContext)
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            DbLogSqlProviderConfigurationElement configurationElement = configurationSection.DbLogSqlProvider as DbLogSqlProviderConfigurationElement;
            string type = configurationElement.Type;
            if (string.IsNullOrWhiteSpace(type)) return null;

            if (type == "XData.Data.Components.DbLogSqlProvider,ElementComponents")
            {
                return new DbLogSqlProvider() { ElementContext = elementContext };
            }

            XElement config = new XElement("dbLogSqlProvider");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            DbLogSqlProvider dbLogSqlProvider = objectCreator.CreateInstance() as DbLogSqlProvider;
            dbLogSqlProvider.ElementContext = elementContext;
            return dbLogSqlProvider;
        }


    }
}
