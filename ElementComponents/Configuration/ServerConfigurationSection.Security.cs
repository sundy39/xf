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
        [ConfigurationProperty("hashers")]
        public HasherConfigurationElementCollection Hashers
        {
            get { return (HasherConfigurationElementCollection)this["hashers"]; }
            set { this["hashers"] = value; }
        }

        [ConfigurationProperty("cryptors")]
        public CryptorConfigurationElementCollection Cryptors
        {
            get { return (CryptorConfigurationElementCollection)this["cryptors"]; }
            set { this["cryptors"] = value; }
        }
    }

    public class HasherConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class HasherConfigurationElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new HasherConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HasherConfigurationElement)element).Name;
        }
    }

    public class CryptorConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }
    }

    public class CryptorConfigurationElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new CryptorConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CryptorConfigurationElement)element).Name;
        }
    }

}
