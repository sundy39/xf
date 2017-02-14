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
    // symmetric 
    public class CryptorManager
    {
        protected Dictionary<string, Cryptor> NameMap = new Dictionary<string, Cryptor>();
        protected Dictionary<Guid, Cryptor> GuidMap = new Dictionary<Guid, Cryptor>();

        public Cryptor this[string name]
        {
            get
            {
                return NameMap[name];
            }
        }

        public Cryptor this[Guid guid]
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

        public CryptorManager()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            foreach (CryptorConfigurationElement element in configurationSection.Cryptors)
            {
                Cryptor cryptor = CreateCryptor(element.Type);
                NameMap[element.Name] = cryptor;
                GuidMap[cryptor.GetType().GUID] = cryptor;
            }
        }

        protected Cryptor CreateCryptor(string type)
        {
            XElement config = new XElement("cryptor");
            config.SetAttributeValue("type", type);
            ObjectCreator objectCreator = new ObjectCreator(config);
            return objectCreator.CreateInstance() as Cryptor;
        }

    }
}
