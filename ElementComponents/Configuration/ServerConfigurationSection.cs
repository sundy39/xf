using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Configuration
{
    public partial class ServerConfigurationSection
    {
        [ConfigurationProperty("authorizationConfigGetterFactory")]
        public AuthorizationConfigGetterFactoryConfigurationElement AuthorizationConfigGetterFactory
        {
            get { return (AuthorizationConfigGetterFactoryConfigurationElement)this["authorizationConfigGetterFactory"]; }
            set { this["authorizationConfigGetterFactory"] = value; }
        }

        [ConfigurationProperty("authorizorFactory")]
        public AuthorizorFactoryConfigurationElement AuthorizorFactory
        {
            get { return (AuthorizorFactoryConfigurationElement)this["authorizorFactory"]; }
            set { this["authorizorFactory"] = value; }
        }

        [ConfigurationProperty("specifiedConfigGetterFactory")]
        public SpecifiedConfigGetterFactoryConfigurationElement SpecifiedConfigGetterFactory
        {
            get { return (SpecifiedConfigGetterFactoryConfigurationElement)this["specifiedConfigGetterFactory"]; }
            set { this["specifiedConfigGetterFactory"] = value; }
        }

        [ConfigurationProperty("dataSourceConfigGetterFactory")]
        public DataSourceConfigGetterFactoryConfigurationElement DataSourceConfigGetterFactory
        {
            get { return (DataSourceConfigGetterFactoryConfigurationElement)this["dataSourceConfigGetterFactory"]; }
            set { this["dataSourceConfigGetterFactory"] = value; }
        }

        [ConfigurationProperty("dataSourceFactory")]
        public DataSourceFactoryConfigurationElement DataSourceFactory
        {
            get { return (DataSourceFactoryConfigurationElement)this["dataSourceFactory"]; }
            set { this["dataSourceFactory"] = value; }
        }

        [ConfigurationProperty("password")]
        public PasswordConfigurationElement Password
        {
            get { return (PasswordConfigurationElement)this["password"]; }
            set { this["password"] = value; }
        }

        [ConfigurationProperty("account")]
        public AccountConfigurationElement Account
        {
            get { return (AccountConfigurationElement)this["account"]; }
            set { this["account"] = value; }
        }

    }

    public class AuthorizationConfigGetterFactoryConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class AuthorizorFactoryConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class SpecifiedConfigGetterFactoryConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class DataSourceConfigGetterFactoryConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class DataSourceFactoryConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class PasswordConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("format", IsRequired = true)]
        public string Format
        {
            get { return (string)this["format"]; }
            set { this["format"] = value; }
        }

        [ConfigurationProperty("algorithmName")]
        public string AlgorithmName
        {
            get { return (string)this["algorithmName"]; }
            set { this["algorithmName"] = value; }
        }

        [ConfigurationProperty("requiredLength")]
        public int RequiredLength
        {
            get { return (int)this["requiredLength"]; }
            set { this["requiredLength"] = value; }
        }

        [ConfigurationProperty("requireDigit", DefaultValue = false)]
        public bool RequireDigit
        {
            get { return (bool)this["requireDigit"]; }
            set { this["requireDigit"] = value; }
        }

        [ConfigurationProperty("requireLowercase", DefaultValue = false)]
        public bool RequireLowercase
        {
            get { return (bool)this["requireLowercase"]; }
            set { this["requireLowercase"] = value; }
        }

        [ConfigurationProperty("requireUppercase", DefaultValue = false)]
        public bool RequireUppercase
        {
            get { return (bool)this["requireUppercase"]; }
            set { this["requireUppercase"] = value; }
        }

        [ConfigurationProperty("requireNonLetterOrDigit", DefaultValue = false)]
        public bool RequireNonLetterOrDigit
        {
            get { return (bool)this["requireNonLetterOrDigit"]; }
            set { this["requireNonLetterOrDigit"] = value; }
        }
    }

    public class AccountConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("maxInvalidAttempts")]
        public int MaxInvalidAttempts
        {
            get { return (int)this["maxInvalidAttempts"]; }
            set { this["maxInvalidAttempts"] = value; }
        }

        [ConfigurationProperty("attemptWindow", DefaultValue = 10)]
        public int AttemptWindow
        {
            get { return (int)this["attemptWindow"]; }
            set { this["attemptWindow"] = value; }
        }
    }

}
