using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public class FilePrimaryConfigGetter : PrimaryConfigGetter
    {
        protected string ConfigVersion { get; private set; }
        protected string FileName { get; private set; }

        public FilePrimaryConfigGetter(string fileName)
        {
            FileName = fileName;
        }

        public FilePrimaryConfigGetter(string configVersion, string fileName)
        {
            ConfigVersion = configVersion;
            FileName = fileName;
        }

        public override XElement GetPrimaryConfig()
        {
            string fileName = FileName;
            if (!File.Exists(fileName))
            {
                fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            }
            XElement primaryConfig = XElement.Load(fileName);

            XAttribute attr = primaryConfig.Attribute(Glossary.ConfigVersion);
            string configVersion = (attr == null) ? null : attr.Value;
            if (string.IsNullOrWhiteSpace(ConfigVersion))
            {
                if (string.IsNullOrWhiteSpace(configVersion)) throw new SchemaException(Messages.ConfigSchema_Version_IsNullOrEmpty, primaryConfig);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(configVersion))
                {
                    primaryConfig.SetAttributeValue(Glossary.ConfigVersion, configVersion);
                }
                else
                {
                    if (configVersion != ConfigVersion) throw new SchemaException(Messages.PrimaryConfig_ConfigVersion_Not_Match_Argument, primaryConfig);
                }
            }
            return primaryConfig;
        }


    }
}
