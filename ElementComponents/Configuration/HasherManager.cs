using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Security;

namespace XData.Data.Configuration
{
    public class HasherManager
    {
        protected Dictionary<string, Hasher> NameMap = new Dictionary<string, Hasher>();
        protected Dictionary<Guid, Hasher> GuidMap = new Dictionary<Guid, Hasher>();

        public Hasher this[string name]
        {
            get
            {
                return NameMap[name];
            }
        }

        public Hasher this[Guid guid]
        {
            get
            {
                return GuidMap[guid];
            }
        }

        public int Count
        {
            get
            {
                return NameMap.Count;
            }
        }

        public HasherManager()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            foreach (HasherConfigurationElement element in configurationSection.Hashers)
            {
                Hasher hasher = CreateHasher(element.Type);
                NameMap[element.Name] = hasher;
                GuidMap[hasher.GetType().GUID] = hasher;
            }
        }

        protected Hasher CreateHasher(string type)
        {
            if (type == "XData.Data.Security.SHA1Hasher,ElementComponents")
            {
                return new SHA1Hasher();
            }
            if (type == "XData.Data.Security.MD5Hasher,ElementComponents")
            {
                return new MD5Hasher();
            }
            if (type == "XData.Data.Security.SHA256Hasher,ElementComponents")
            {
                return new SHA256Hasher();
            }
            if (type == "XData.Data.Security.SHA384Hasher,ElementComponents")
            {
                return new SHA384Hasher();
            }
            if (type == "XData.Data.Security.SHA512Hasher,ElementComponents")
            {
                return new SHA512Hasher();
            }
            XElement config = new XElement("hasher");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            return objectCreator.CreateInstance() as Hasher;
        }

    }
}
