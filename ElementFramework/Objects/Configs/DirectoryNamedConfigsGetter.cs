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
    public class DirectoryNamedConfigsGetter : NamedConfigsGetter
    {
        public string Directory { get; private set; }

        public DirectoryNamedConfigsGetter(string directory)
        {
            Directory = directory;
        }

        public override IEnumerable<XElement> GetNamedConfigs()
        {
            string dir = Directory;
            if (!System.IO.Directory.Exists(dir))
            {
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
            }
            string[] fileNames = System.IO.Directory.GetFiles(dir);

            string primaryVersion = PrimarySchema.Attribute("Version").Value;

            List<XElement> namedConfigs = new List<XElement>();
            foreach (string fileName in fileNames)
            {
                string extension = Path.GetExtension(fileName);
                if (extension.ToLower() == ".config")
                {
                    XElement namedConfig = null;
                    try
                    {
                        namedConfig = XElement.Load(fileName);
                    }
                    catch
                    {
                    }
                    if (namedConfig == null) continue;

                    string name = (namedConfig.Attribute("Name") == null) ? null : namedConfig.Attribute("Name").Value;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    string version = (namedConfig.Attribute("Version") == null) ? null : namedConfig.Attribute("Version").Value;
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        if (version != primaryVersion) continue;
                    }

                    namedConfigs.Add(namedConfig);
                }
            }
            return namedConfigs;
        }


    }
}
