using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema.Validation;

namespace XData.Data.Schema
{
    public class SchemaManager
    {
        public PrimarySchemaObject PrimarySchemaObject
        {
            get;
            private set;
        }

        public XElement PrimarySchema
        {
            get { return PrimarySchemaObject.PrimarySchema; }
        }
        protected readonly string DatabaseKey;

        private readonly IEnumerable<XElement> _namedConfigs;
        public IEnumerable<XElement> NamedConfigs
        {
            get
            {
                XElement schemas = new XElement("Schemas");
                schemas.Add(_namedConfigs);
                return new XElement(schemas).Elements();
            }
        }

        public IEnumerable<XElement> NamedSchemas
        {
            get
            {
                XElement schemas = new XElement("Schemas");
                schemas.Add(NamedSchemasDicts[DatabaseKey].Values);
                return new XElement(schemas).Elements();
            }
        }

        public IEnumerable<XElement> NamedPublishSchemas
        {
            get
            {
                XElement schemas = new XElement("Schemas");
                schemas.Add(PublishSchemasDicts[DatabaseKey].Values);
                return new XElement(schemas).Elements();
            }
        }

        protected readonly static Dictionary<string, string> CollectionKeys = new Dictionary<string, string>();
        protected readonly static Dictionary<string, Dictionary<string, XElement>> NamedSchemasDicts = new Dictionary<string, Dictionary<string, XElement>>();
        protected readonly static Dictionary<string, Dictionary<string, XElement>> PublishSchemasDicts = new Dictionary<string, Dictionary<string, XElement>>();

        public SchemaManager(PrimarySchemaObject primarySchemaObject, IEnumerable<XElement> namedConfigs)
        {
            PrimarySchemaObject = primarySchemaObject;
            _namedConfigs = (namedConfigs == null) ? new XElement("NamedConfigs").Elements() : namedConfigs;

            DatabaseKey = PrimarySchema.Attribute(Glossary.DatabaseKey).Value;
            string version = PrimarySchema.Attribute(Glossary.SchemaVersion).Value;
            IEnumerable<string> array = _namedConfigs.OrderBy(x => x.Attribute("Name").Value).Select(x =>
                string.Format("Name=\"{0}\" NamedConfigVersion=\"{1}\"",
                x.Attribute("Name").Value, x.Attribute("NamedConfigVersion").Value)).ToArray();
            string collectionKey = string.Join(";", array);
            collectionKey = version + ";" + collectionKey;
            if (NamedSchemasDicts.ContainsKey(DatabaseKey) && CollectionKeys[DatabaseKey] == collectionKey) return;

            lock (CollectionKeys)
            {
                CollectionKeys[DatabaseKey] = collectionKey;

                Dictionary<string, XElement> namedSchemasDict;
                Dictionary<string, XElement> publishSchemasDict;

                List<XElement> configs = new List<XElement>(_namedConfigs);
                if (NamedSchemasDicts.ContainsKey(DatabaseKey))
                {
                    namedSchemasDict = NamedSchemasDicts[DatabaseKey];
                    publishSchemasDict = PublishSchemasDicts[DatabaseKey];

                    List<string> deleting = new List<string>();
                    foreach (KeyValuePair<string, XElement> pair in namedSchemasDict)
                    {
                        if (pair.Value.Attribute(Glossary.SchemaVersion).Value != version)
                        {
                            deleting.Add(pair.Key);
                            continue;
                        }
                        string namedConfigVersion = pair.Value.Attribute("NamedConfigVersion").Value;
                        XElement config = configs.FirstOrDefault(x => x.Attribute("Name").Value == pair.Key &&
                            x.Attribute("NamedConfigVersion").Value == namedConfigVersion);
                        if (config == null)
                        {
                            deleting.Add(pair.Key);
                        }
                        else
                        {
                            config.Remove();
                        }
                    }
                    foreach (string key in deleting)
                    {
                        namedSchemasDict.Remove(key);
                        publishSchemasDict.Remove(key);
                    }
                }
                else
                {
                    namedSchemasDict = new Dictionary<string, XElement>();
                    NamedSchemasDicts[DatabaseKey] = namedSchemasDict;
                    publishSchemasDict = new Dictionary<string, XElement>();
                    PublishSchemasDicts[DatabaseKey] = publishSchemasDict;
                }
                foreach (XElement config in configs)
                {
                    XElement namedSchema = CreateNamedSchema(PrimarySchema, config);
                    string name = config.Attribute("Name").Value;
                    namedSchemasDict[name] = namedSchema;
                    publishSchemasDict[name] = namedSchema.CreatePublishSchema();
                }
            }
        }

        protected static XElement CreateNamedSchema(XElement primarySchema, XElement namedConfig)
        {
            XElement schema = new XElement(primarySchema);
            string version = primarySchema.Attribute(Glossary.SchemaVersion).Value;

            //
            XAttribute attr = attr = namedConfig.Attribute(Glossary.SchemaVersion);
            if (attr == null)
            {
                schema.SetAttributeValue(Glossary.SchemaVersion, version);
            }
            else
            {
                if (attr.Value != version)
                    throw new SchemaException(Messages.NamedConfig_Not_Match_PrimaryVersion, namedConfig);
            }

            //
            attr = namedConfig.Attribute("Name");
            string name = (attr == null) ? null : attr.Value;
            if (string.IsNullOrWhiteSpace(name)) throw new SchemaException(Messages.Name_IsNullOrEmpty, namedConfig);
            schema.SetAttributeValue("Name", name);

            attr = namedConfig.Attribute("NamedConfigVersion");
            string namedConfigVersion = (attr == null) ? null : attr.Value;
            if (string.IsNullOrWhiteSpace(namedConfigVersion)) throw new SchemaException(Messages.NamedConfigVersion_IsNullOrEmpty, namedConfig);
            schema.SetAttributeValue("NamedConfigVersion", namedConfigVersion);

            //
            schema.Modify(namedConfig);

            // ManyToMany
            IEnumerable<XElement> manyToManyRelationships = schema.DeduceOutManyToManyRelationships();
            schema.Add(manyToManyRelationships);

            //
            var errors = schema.GetValidationErrors();
            if (errors.Count() == 0) return schema;

            throw new SchemaValidationException(schema.Attribute("Version").Value, schema.Attribute("Name").Value, errors);
        }

        public XElement GetNamedConfig(string name)
        {
            XElement namedConfig = _namedConfigs.FirstOrDefault(x => x.Attribute("Name").Value == name);
            if (namedConfig == null) return null;
            return new XElement(namedConfig);
        }

        protected XElement GetNamedSchema(string name)
        {
            return new XElement(NamedSchemasDicts[DatabaseKey][name]);
        }

        public XElement GetSchema(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return PrimarySchema;
            return GetNamedSchema(name);
        }

        public XElement GetPublishSchema(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return PrimarySchemaObject.PublishSchema;
            return new XElement(PublishSchemasDicts[DatabaseKey][name]);
        }

        public XElement GetWarnings(string name)
        {
            return GetSchema(name).GetWarnings();
        }


    }
}
