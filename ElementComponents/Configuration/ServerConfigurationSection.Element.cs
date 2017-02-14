using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Configuration
{
    public partial class ServerConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("currentUserIdentityGetter")]
        public CurrentUserIdentityGetterConfigurationElement CurrentUserIdentityGetter
        {
            get { return (CurrentUserIdentityGetterConfigurationElement)this["currentUserIdentityGetter"]; }
            set { this["currentUserIdentityGetter"] = value; }
        }

        [ConfigurationProperty("elementValidator")]
        public ElementValidatorConfigurationElement ElementValidator
        {
            get { return (ElementValidatorConfigurationElement)this["elementValidator"]; }
            set { this["elementValidator"] = value; }
        }

        [ConfigurationProperty("commonFieldsSetter")]
        public CommonFieldsSetterConfigurationElement CommonFieldsSetter
        {
            get { return (CommonFieldsSetterConfigurationElement)this["commonFieldsSetter"]; }
            set { this["commonFieldsSetter"] = value; }
        }

        [ConfigurationProperty("dbLogSqlProvider")]
        public DbLogSqlProviderConfigurationElement DbLogSqlProvider
        {
            get { return (DbLogSqlProviderConfigurationElement)this["dbLogSqlProvider"]; }
            set { this["dbLogSqlProvider"] = value; }
        }

        [ConfigurationProperty("elementContextFactory", IsRequired = true)]
        public ElementContextFactoryConfigurationElement ElementContextFactory
        {
            get { return (ElementContextFactoryConfigurationElement)this["elementContextFactory"]; }
            set { this["elementContextFactory"] = value; }
        }

    }

    public class CurrentUserIdentityGetterConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class ElementValidatorConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class CommonFieldsSetterConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class DbLogSqlProviderConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class ElementContextFactoryConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

}
