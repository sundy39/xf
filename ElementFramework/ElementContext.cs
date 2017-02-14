using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public partial class ElementContext
    {
        public Database Database { get; private set; }

        public SchemaManager SchemaManager { get; private set; }

        public XElement PrimarySchema
        {
            get { return SchemaManager.PrimarySchema; }
        }

        public XElement GetSchema(string name)
        {
            return SchemaManager.GetSchema(name);
        }

        public ElementContext(Database database, NameMap nameMap, XElement databaseConfig, XElement primaryConfig, IEnumerable<XElement> namedConfigs)
        {
            Database = database;

            // check DatabaseVersion of nameMap
            XAttribute attr = nameMap.Config.Attribute(Glossary.DatabaseVersion);
            if (attr != null)
            {
                if (attr.Value != Database.DatabaseVersion)
                {
                    throw new SchemaException(Messages.NameMap_Not_Match_DatabaseVersion, nameMap.Config);
                }
            }

            DatabaseSchemaObject databaseSchemaObject = new DatabaseSchemaObject(database, nameMap);
            databaseSchemaObject.Modify(databaseConfig);

            // check DatabaseVersion and NameMapVersion of primaryConfig
            attr = primaryConfig.Attribute(Glossary.DatabaseVersion);
            if (attr != null)
            {
                if (attr.Value != Database.DatabaseVersion)
                {
                    throw new SchemaException(Messages.PrimaryConfig_Not_Match_DatabaseVersion, primaryConfig);
                }
            }
            attr = primaryConfig.Attribute(Glossary.NameMapVersion);
            if (attr != null)
            {
                if (attr.Value != nameMap.NameMapVersion)
                {
                    throw new SchemaException(Messages.PrimaryConfig_Not_Match_NameMap, primaryConfig);
                }
            }

            PrimarySchemaObject primarySchemaObject = new PrimarySchemaObject(databaseSchemaObject, primaryConfig);

            // check Version of namedConfig
            if (namedConfigs != null)
            {
                foreach (XElement namedConfig in namedConfigs)
                {
                    attr = namedConfig.Attribute("Name");
                    string nameOfNamed = (attr == null) ? null : attr.Value;
                    if (string.IsNullOrWhiteSpace(nameOfNamed))
                        throw new SchemaException(Messages.NamedConfig_Name_IsNullOrEmpty, namedConfig);

                    attr = namedConfig.Attribute("Version");
                    if (attr != null)
                    {
                        string version = primarySchemaObject.PrimarySchema.Attribute("Version").Value;
                        if (attr.Value != version)
                        {
                            throw new SchemaException(Messages.NamedConfig_Not_Match_PrimaryVersion, namedConfig);
                        }
                    }
                }
            }

            //
            SchemaManager = new SchemaManager(primarySchemaObject, namedConfigs);

            //
            Reader = new Reader(Database);

            //
            Writer = new Writer(Database);
            Writer.Validating += (sender, args) => { OnValidating(args); };
        }


    }
}
