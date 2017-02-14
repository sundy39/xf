using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Configuration;
using XData.Data.Element;
using XData.Data.Schema;

namespace XData.Data.Services
{
    public class SchemasService
    {
        protected ElementContext ElementContext = ConfigurationCreator.CreateElementContext();

        protected SchemaManager SchemaManager
        {
            get
            {
                return ElementContext.SchemaManager;
            }
        }

        public IEnumerable<XElement> GetNamedConfigs()
        {
            return SchemaManager.NamedConfigs;
        }

        public XElement GetNamedConfig(string name)
        {
            return SchemaManager.GetNamedConfig(name);
        }

        public XElement GetPublishSchema()
        {
            return SchemaManager.PrimarySchemaObject.PublishSchema;
        }

        public XElement GetPrimarySchema()
        {
            return SchemaManager.PrimarySchemaObject.PrimarySchema;
        }

        public XElement GetPrimaryConfig()
        {
            return SchemaManager.PrimarySchemaObject.PrimaryConfig;
        }

        public XElement GetNameMapConfig()
        {
            return SchemaManager.PrimarySchemaObject.NameMapConfig;
        }

        public XElement GetDatabaseSchema()
        {
            return SchemaManager.PrimarySchemaObject.DatabaseSchema;
        }

        public XElement GetWarnings()
        {
            return SchemaManager.PrimarySchemaObject.GetWarnings();
        }

        public IEnumerable<XElement> GetNamedPublishSchemas()
        {
            return SchemaManager.NamedPublishSchemas;
        }

        public XElement GetPublishSchema(string name)
        {
            return SchemaManager.GetPublishSchema(name);
        }

        public IEnumerable<XElement> GetNamedSchemas()
        {
            return SchemaManager.NamedSchemas;
        }

        public XElement GetWarnings(string name)
        {
            return SchemaManager.GetWarnings(name);
        }

        public XElement GetSchema(string name)
        {
            return SchemaManager.GetSchema(name);
        }


    }
}
