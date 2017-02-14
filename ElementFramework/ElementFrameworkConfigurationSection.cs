using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Configuration
{
    public class ElementFrameworkConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("instances", IsRequired = true)]
        public InstancesConfigurationElement Instances
        {
            get { return (InstancesConfigurationElement)this["instances"]; }
            set { this["instances"] = value; }
        }
    }

    public class InstancesConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("directory", IsRequired = true)]
        public string Directory
        {
            get { return (string)this["directory"]; }
            set { this["directory"] = value; }
        }

        [ConfigurationProperty("watch", DefaultValue = false, IsRequired = false)]
        public bool Watch
        {
            get { return (bool)this["watch"]; }
            set { this["watch"] = value; }
        }

        [ConfigurationProperty("delay", DefaultValue = 1000, IsRequired = false)]
        public int Delay
        {
            get { return (int)this["delay"]; }
            set { this["delay"] = value; }
        }


    }
}
