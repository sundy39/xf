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
    public class FileNameMapConfigGetter : NameMapConfigGetter
    {
        public string FileName { get; private set; }

        public FileNameMapConfigGetter(string fileName)
        {
            FileName = fileName;
        }

        public override XElement GetNameMapConfig()
        {
            string fileName = FileName;
            if (!File.Exists(fileName))
            {
                fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            }
            XElement config = XElement.Load(fileName);
            XAttribute attr = config.Attribute(Glossary.NameMapVersion);
            string configVersion = (attr == null) ? null : attr.Value;
            if (string.IsNullOrWhiteSpace(configVersion)) throw new SchemaException(Messages.NameMap_Version_IsNullOrEmpty, config);
            return config;
        }


    }
}
