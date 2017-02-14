using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Resources;
using XData.Data.Schema.Validation;

namespace XData.Data.Schema
{
    public class PrimarySchemaObject
    {
        public XElement PublishSchema { get { return new XElement(GetPublishSchema()); } }
        public XElement PrimarySchema { get { return new XElement(GetPrimarySchema()); } }
        public XElement DatabaseSchema { get { return DatabaseSchemaObject.DatabaseSchema; } }

        public NameMap NameMap { get { return DatabaseSchemaObject.NameMap; } }
        public XElement NameMapConfig { get { return new XElement(NameMap.Config); } }

        internal DatabaseSchemaObject DatabaseSchemaObject;

        private readonly XElement _primaryConfig;
        public XElement PrimaryConfig { get { return new XElement(_primaryConfig); } }

        protected readonly static Dictionary<string, XElement> PrimarySchemas = new Dictionary<string, XElement>();
        protected readonly static Dictionary<string, XElement> PublishSchemas = new Dictionary<string, XElement>();

        protected XElement GetPrimarySchema()
        {
            XElement databaseSchema = DatabaseSchemaObject.DatabaseSchema;
            string databaseKey = databaseSchema.Attribute(Glossary.DatabaseKey).Value;
            if (PrimarySchemas.ContainsKey(databaseKey))
            {
                XElement cachedPrimarySchema = PrimarySchemas[databaseKey];
                if (cachedPrimarySchema.Attribute(Glossary.DatabaseVersion).Value == databaseSchema.Attribute(Glossary.DatabaseVersion).Value &&
                    cachedPrimarySchema.Attribute(Glossary.NameMapVersion).Value == databaseSchema.Attribute(Glossary.NameMapVersion).Value &&
                    cachedPrimarySchema.Attribute(Glossary.ConfigVersion).Value == PrimaryConfig.Attribute(Glossary.ConfigVersion).Value)
                {
                    return cachedPrimarySchema;
                }
            }

            lock (PrimarySchemas)
            {
                XElement primarySchema = MergeWithPrimaryConfig(databaseSchema, PrimaryConfig);

                // ManyToMany
                IEnumerable<XElement> manyToManyRelationships = primarySchema.DeduceOutManyToManyRelationships();
                primarySchema.Add(manyToManyRelationships);

                PrimarySchemas[databaseKey] = primarySchema;
                PublishSchemas[databaseKey] = primarySchema.CreatePublishSchema();
            }

            return PrimarySchemas[databaseKey];
        }

        protected static XElement MergeWithPrimaryConfig(XElement databaseSchema, XElement primaryConfig)
        {
            XElement schema = new XElement(databaseSchema);

            // nameMapVersion
            string databaseVersion = schema.Attribute(Glossary.DatabaseVersion).Value;
            string mappingVersion = schema.Attribute(Glossary.NameMapVersion).Value;
            string configVersion = primaryConfig.Attribute(Glossary.ConfigVersion).Value;
            schema.SetAttributeValue(Glossary.ConfigVersion, configVersion);
            schema.SetAttributeValue(Glossary.SchemaVersion, string.Format("{0}.{1}.{2}", databaseVersion, mappingVersion, configVersion));

            // merge
            XElement removeConfig = new XElement("config");
            List<XElement> incremElements = new List<XElement>();
            List<XElement> incremRelationships = new List<XElement>();
            foreach (XElement configElement in primaryConfig.Elements())
            {
                if (configElement.Name.LocalName == Glossary.Remove)
                {
                    removeConfig.Add(configElement);
                    continue;
                }
                if(configElement.Name.LocalName == Glossary.ReferencePath &&
                    configElement.Attribute(Glossary.RelationshipContent) != null)
                {
                    continue;
                }
                if (configElement.Name.LocalName == Glossary.Relationship &&
                    configElement.Attribute(Glossary.RelationshipContent) != null)
                {
                    string relationshipType = configElement.Attribute(Glossary.RelationshipType).Value;
                    if (relationshipType == "ManyToOne" || relationshipType == "ManyToMany")
                    {
                        XElement schemaRelationship = schema.Elements(Glossary.Relationship).FirstOrDefault(p =>
                            p.Attribute(Glossary.RelationshipContent) != null
                            && p.Attribute(Glossary.RelationshipContent).Value == configElement.Attribute(Glossary.RelationshipContent).Value);
                        if (schemaRelationship != null)
                        {
                            schemaRelationship.Add(configElement.Elements());
                            continue;
                        }
                    }
                    Debug.Assert(configElement.Attribute(Glossary.RelationshipType) != null);
                    incremRelationships.Add(configElement);
                }
                else
                {
                    XElement schemaElement = schema.Element(configElement.Name);

                    if (schemaElement == null)
                    {
                        Debug.Assert(configElement.Attribute(Glossary.Set) != null);

                        Debug.Assert(configElement.Attribute("PrimaryKey") != null);

                        incremElements.Add(configElement);
                    }
                    else
                    {
                        if (schemaElement.Attribute(Glossary.Set) == null) continue;

                        foreach (XElement configItem in configElement.Elements())
                        {
                            if (schemaElement.Element(configItem.Name) == null)
                            {
                                schemaElement.Add(configItem);
                            }
                            else
                            {
                                schemaElement.Element(configItem.Name).Add(configItem.Elements());
                            }
                        }
                    }
                }
            }

            XElement schema1 = new XElement(schema);
            schema.RemoveNodes();

            schema.Add(schema1.Elements().Where(p => p.Attribute(Glossary.Set) != null));
            schema.Add(incremElements.OrderBy(p => p.Name.LocalName));

            schema.Add(schema1.Elements(Glossary.Relationship).Where(p => p.Attribute(Glossary.RelationshipContent) != null));
            schema.Add(incremRelationships.OrderBy(p => p.Attribute(Glossary.RelationshipFrom).Value)
                .ThenBy(p => p.Attribute(Glossary.RelationshipTo).Value)
                .ThenBy(p => p.Attribute(Glossary.RelationshipType).Value));

            schema.Add(primaryConfig.Elements(Glossary.ReferencePath).Where(p => p.Attribute("Content") != null));

            var errors = schema.GetValidationErrors();
            if (errors.Count() == 0)
            {
                schema.Modify(removeConfig);
                return schema;
            }

            throw new SchemaValidationException(schema.Attribute("Version").Value, errors);
        }

        protected XElement GetPublishSchema()
        {
            GetPrimarySchema();
            XElement databaseSchema = DatabaseSchemaObject.DatabaseSchema;
            string databaseKey = databaseSchema.Attribute(Glossary.DatabaseKey).Value;
            return PublishSchemas[databaseKey];
        }

        public PrimarySchemaObject(DatabaseSchemaObject databaseSchemaObject, XElement primaryConfig)
        {
            DatabaseSchemaObject = databaseSchemaObject;
            _primaryConfig = primaryConfig;
        }

        public XElement GetWarnings()
        {
            return this.PrimarySchema.GetWarnings();
        }


    }
}
