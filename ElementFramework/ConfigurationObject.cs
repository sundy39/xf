using System.Collections.Generic;
using System.Configuration;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using XData.Data;
using XData.Data.Objects;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public partial class ElementContext
    {
        protected class ConfigurationObject
        {
            public XElement Config { get; private set; }
            public ObjectCreator DatabaseCreator { get; private set; }
            public ObjectCreator NameMapCreator { get; private set; }
            public XElement PrimaryConfig { get; private set; }
            public XElement NamedConfigs { get; private set; }

            public ConfigurationObject(XElement config)
            {
                Config = new XElement(config);

                //
                XElement databaseConfig = config.Element("database");
                DatabaseCreator = CreateDatabaseCreator(databaseConfig);
                Database database = DatabaseCreator.CreateInstance() as Database;

                //
                XElement nameMapConfig = config.Element("nameMap");
                NameMapCreator = CreateNameMapCreator(nameMapConfig);
                NameMap nameMap = NameMapCreator.CreateInstance() as NameMap;

                // check DatabaseVersion of nameMap
                XAttribute attr = nameMap.Config.Attribute(Glossary.DatabaseVersion);
                if (attr != null)
                {
                    if (attr.Value != database.DatabaseVersion)
                    {
                        throw new SchemaException(Messages.NameMap_Not_Match_DatabaseVersion, nameMap.Config);
                    }
                }

                DatabaseSchemaObject databaseSchemaObject = new DatabaseSchemaObject(database, nameMap);

                // DatabaseConfig
                XElement dbConfig = null;
                XElement databaseConfigGetterConfig = config.Element("databaseConfigGetter");
                if (databaseConfigGetterConfig != null)
                {
                    dbConfig = GetDatabaseConfig(databaseConfigGetterConfig, databaseSchemaObject.DatabaseSchema);
                    attr = dbConfig.Attribute(Glossary.NameMapVersion);
                    if (attr != null)
                    {
                        if (attr.Value != nameMap.NameMapVersion)
                        {
                            throw new SchemaException(Messages.DatabaseConfig_Not_Match_NameMap, dbConfig);
                        }
                    }                   
                }
                databaseSchemaObject.Modify(dbConfig);

                // PrimaryConfig               
                XElement primaryConfigGetterConfig = config.Element("primaryConfigGetter");
                XElement primaryConfig = GetPrimaryConfig(primaryConfigGetterConfig, databaseSchemaObject.DatabaseSchema);
                attr = primaryConfig.Attribute(Glossary.NameMapVersion);
                if (attr != null)
                {
                    if (attr.Value != nameMap.NameMapVersion)
                    {
                        throw new SchemaException(Messages.PrimaryConfig_Not_Match_NameMap, primaryConfig);
                    }
                }
                PrimaryConfig = primaryConfig;

                // NamedConfigs
                NamedConfigs = new XElement("NamedConfigs");
                XElement namedConfigsGetterConfig = config.Element("namedConfigsGetter");
                if (namedConfigsGetterConfig != null)
                {
                    PrimarySchemaObject primarySchemaObject = new PrimarySchemaObject(databaseSchemaObject, PrimaryConfig);
                    IEnumerable<XElement> namedConfigs = GetNamedConfigs(namedConfigsGetterConfig, primarySchemaObject.PrimarySchema);
                    NamedConfigs.Add(namedConfigs);
                }
            }

            protected ObjectCreator CreateDatabaseCreator(XElement databaseConfig)
            {
                XElement config = new XElement(databaseConfig);
                XElement connNameNode = config.Element("connectionStringName");
                if (connNameNode != null)              
                {
                    string connectionStringName = connNameNode.Attribute("value").Value;
                    string connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
                    connNameNode.Attribute("value").Value = connectionString;
                    connNameNode.Name = "connectionString";
                }
                ObjectCreator objectCreator = new ObjectCreator(config);
                return objectCreator;
            }

            protected ObjectCreator CreateNameMapCreator(XElement nameMapConfig)
            {
                XElement config = new XElement(nameMapConfig);
                XAttribute attr = config.Element("nameMapConfig").Attribute("getter");
                if (attr != null)
                {
                    XElement getterConfig = Config.Element(attr.Value);
                    NameMapConfigGetter nameMapConfigGetter = new ObjectCreator(getterConfig).CreateInstance() as NameMapConfigGetter;
                    config.Element("nameMapConfig").Add(nameMapConfigGetter.GetNameMapConfig());
                    attr.Remove();
                }
                ObjectCreator objectCreator = new ObjectCreator(config);
                return objectCreator;
            }

            protected XElement GetDatabaseConfig(XElement databaseConfigGetterConfig, XElement databaseSchema)
            {
                DatabaseConfigGetter databaseConfigGetter = new ObjectCreator(new XElement(databaseConfigGetterConfig)).CreateInstance() as DatabaseConfigGetter;
                databaseConfigGetter.DatabaseSchema = databaseSchema;
                XElement databaseConfig = databaseConfigGetter.GetDatabaseConfig();

                // check DatabaseVersion and NameMapVersion of config
                XAttribute attr = databaseConfig.Attribute(Glossary.DatabaseVersion);
                if (attr != null)
                {
                    string databaseVersion = databaseSchema.Attribute(Glossary.DatabaseVersion).Value;
                    if (attr.Value != databaseVersion)
                    {
                        throw new SchemaException(Messages.DatabaseConfig_Not_Match_DatabaseVersion, databaseConfig);
                    }
                }
                return databaseConfig;
            }

            protected XElement GetPrimaryConfig(XElement primaryConfigGetterConfig, XElement databaseSchema)
            {
                PrimaryConfigGetter primaryConfigGetter = new ObjectCreator(new XElement(primaryConfigGetterConfig)).CreateInstance() as PrimaryConfigGetter;
                primaryConfigGetter.DatabaseSchema = databaseSchema;
                XElement primaryConfig = primaryConfigGetter.GetPrimaryConfig();

                // check DatabaseVersion and NameMapVersion of config
                XAttribute attr = primaryConfig.Attribute(Glossary.DatabaseVersion);
                if (attr != null)
                {
                    string databaseVersion = databaseSchema.Attribute(Glossary.DatabaseVersion).Value;
                    if (attr.Value != databaseVersion)
                    {
                        throw new SchemaException(Messages.PrimaryConfig_Not_Match_DatabaseVersion, primaryConfig);
                    }
                }
                return primaryConfig;
            }

            protected IEnumerable<XElement> GetNamedConfigs(XElement namedConfigsGetterConfig, XElement primarySchema)
            {
                NamedConfigsGetter namedConfigsGetter = new ObjectCreator(new XElement(namedConfigsGetterConfig)).CreateInstance() as NamedConfigsGetter;
                namedConfigsGetter.PrimarySchema = primarySchema;
                IEnumerable<XElement> namedConfigs = namedConfigsGetter.GetNamedConfigs();

                // check Version of namedConfig
                foreach (XElement namedConfig in namedConfigs)
                {
                    XAttribute attr = namedConfig.Attribute("Name");
                    string nameOfNamed = (attr == null) ? null : attr.Value;
                    if (string.IsNullOrWhiteSpace(nameOfNamed))
                        throw new SchemaException(Messages.NamedConfig_Name_IsNullOrEmpty, namedConfig);

                    attr = namedConfig.Attribute("Version");
                    if (attr != null)
                    {
                        string version = primarySchema.Attribute("Version").Value;
                        if (attr.Value != version)
                        {
                            throw new SchemaException(Messages.NamedConfig_Not_Match_PrimaryVersion, namedConfig);
                        }
                    }
                }
                return namedConfigs;
            }


        }
    }
}
