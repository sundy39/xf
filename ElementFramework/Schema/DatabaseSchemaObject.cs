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
    public class DatabaseSchemaObject
    {
        public XElement DatabaseSchema { get { return new XElement(GetDatabaseSchema()); } }

        protected Database Database { get; private set; }

        public NameMap NameMap { get; private set; }

        public DatabaseSchemaObject(Database database, NameMap nameMap)
        {
            Database = database;
            NameMap = nameMap;
        }

        protected readonly static Dictionary<string, XElement> DatabaseSchemas = new Dictionary<string, XElement>();

        protected XElement GetDatabaseSchema()
        {
            DataSet dataSet = Database.GetSchemaDataSet();
            string databaseKey = dataSet.ExtendedProperties[Glossary.DatabaseKey].ToString();
            if (DatabaseSchemas.ContainsKey(databaseKey))
            {
                XElement cachedDatabaseSchema = DatabaseSchemas[databaseKey];

                if (cachedDatabaseSchema.Attribute(Glossary.DatabaseVersion).Value == dataSet.ExtendedProperties[Glossary.DatabaseVersion].ToString() &&
                    cachedDatabaseSchema.Attribute(Glossary.NameMapVersion).Value == NameMap.NameMapVersion)
                {
                    return cachedDatabaseSchema;
                }
            }

            lock (DatabaseSchemas)
            {
                XElement databaseSchema = CreateDatabaseSchema(dataSet, NameMap);
                AttachAttributes(databaseSchema);
                DatabaseSchemas[databaseKey] = databaseSchema;
            }

            return DatabaseSchemas[databaseKey];
        }

        protected static void AttachAttributes(XElement databaseSchema)
        {
            IEnumerable<XElement> elementSchemas = databaseSchema.Elements().Where(x => x.Attribute(Glossary.Set) != null);

            foreach (XElement elementSchema in elementSchemas)
            {
                string primaryKey = (elementSchema.Attribute("PrimaryKey") == null) ? string.Empty : elementSchema.Attribute("PrimaryKey").Value;
                string[] keyColumns = primaryKey.Split(',');

                //
                foreach (XElement fieldSchema in elementSchema.Elements())
                {
                    string columnName = (fieldSchema.Attribute("Column") == null) ? fieldSchema.Name.LocalName : fieldSchema.Attribute("Column").Value;

                    // Key
                    if (keyColumns.Contains(columnName))
                    {
                        XElement attrElmt = new XElement("Key");
                        fieldSchema.Add(attrElmt);
                    }
                    else
                    {
                        // Required
                        if (fieldSchema.Attribute("AllowDBNull").Value == false.ToString())
                        {
                            XElement attrElmt = new XElement("Required");
                            fieldSchema.Add(attrElmt);
                        }
                    }

                    // Timestamp
                    if (fieldSchema.Attribute("SqlDbType").Value.ToLower() == "timestamp" || fieldSchema.Attribute("SqlDbType").Value.ToLower() == "rowversion")
                    {
                        XElement attrElmt = new XElement("Timestamp");
                        fieldSchema.Add(attrElmt);
                    }

                    // DefaultValue
                    if (fieldSchema.Attribute("DefaultValue") != null)
                    {
                        XElement attrElmt = new XElement("DefaultValue");
                        attrElmt.Add(new XElement("Value", fieldSchema.Attribute("DefaultValue").Value));
                        fieldSchema.Add(attrElmt);
                    }
                }
            }

            // ManyToMany
            //IEnumerable<XElement> manyToOneRelationship = databaseSchema.Elements(Glossary.Relationship).Where(x => x.Attribute(Glossary.RelationshipContent) != null).ToList();
            //IEnumerable<XElement> manyToManyRelationships = databaseSchema.DeduceOutManyToManyRelationships();
            //databaseSchema.Add(manyToManyRelationships);
        }

        protected static XElement CreateDatabaseSchema(DataSet dataSet, NameMap nameMap)
        {
            List<XElement> elements = new List<XElement>();
            List<XElement> relationships = new List<XElement>();

            foreach (DataTable table in dataSet.Tables)
            {
                XElement element = new XElement(nameMap.GetElementName(table.TableName));
                element.SetAttributeValue(Glossary.Table, table.TableName);
                foreach (DataColumn column in table.Columns)
                {
                    XElement fieldElement = new XElement(nameMap.GetFieldName(table.TableName, column.ColumnName));
                    if (fieldElement.Name.LocalName != column.ColumnName)
                    {
                        fieldElement.SetAttributeValue(Glossary.Column, column.ColumnName);
                    }
                    fieldElement.SetAttributeValue("DataType", column.DataType);
                    fieldElement.SetAttributeValue("AllowDBNull", column.AllowDBNull.ToString());
                    fieldElement.SetAttributeValue("Unique", column.Unique.ToString());
                    fieldElement.SetAttributeValue("ReadOnly", column.ReadOnly.ToString());
                    if (column.AutoIncrement)
                    {
                        fieldElement.SetAttributeValue("AutoIncrement", column.AutoIncrement.ToString());
                    }
                    if (column.ExtendedProperties.ContainsKey("Sequence"))
                    {
                        fieldElement.SetAttributeValue("Sequence", column.ExtendedProperties["Sequence"].ToString());
                    }
                    if (column.DataType == typeof(DateTime))
                    {
                        fieldElement.SetAttributeValue("DateTimeMode", column.DateTimeMode.ToString());
                    }
                    if (column.DefaultValue == DBNull.Value)
                    {
                        if (column.ExtendedProperties.ContainsKey("DefaultValue"))
                        {
                            fieldElement.SetAttributeValue("DefaultValue", column.ExtendedProperties["DefaultValue"].ToString());
                        }
                    }
                    else
                    {
                        fieldElement.SetAttributeValue("DefaultValue", column.DefaultValue.ToString());
                    }

                    string sqlDbType = column.ExtendedProperties["DataType"].ToString().ToLower();
                    fieldElement.SetAttributeValue("SqlDbType", sqlDbType);

                    element.Add(fieldElement);
                }

                // View maybe not PrimaryKey
                if (table.PrimaryKey.Length > 0)
                {
                    string primaryKey = table.PrimaryKey.Select(p => p.ColumnName).Aggregate((p, v) => string.Format("{0},{1}", p, v));
                    element.SetAttributeValue("PrimaryKey", primaryKey);
                }

                // RowVersion
                if (table.ExtendedProperties.ContainsKey("RowVersion"))
                {
                    element.SetAttributeValue("RowVersion", nameMap.GetFieldName(table.TableName, table.ExtendedProperties["RowVersion"].ToString()));
                }

                // Set Id
                element.SetAttributeValue(Glossary.Set, nameMap.GetSetName(nameMap.GetElementName(table.TableName)));

                // ExtendedProperties["TableType"]
                element.SetAttributeValue(Glossary.TableType, table.ExtendedProperties["TableType"].ToString());

                elements.Add(element);

                // ForeignKey -> Relationship
                foreach (Constraint constraint in table.Constraints)
                {
                    if (constraint is ForeignKeyConstraint)
                    {
                        ForeignKeyConstraint foreignKeyConstraint = constraint as ForeignKeyConstraint;
                        string elementName = nameMap.GetElementName(foreignKeyConstraint.Table.TableName);
                        string relatedElementName = nameMap.GetElementName(foreignKeyConstraint.RelatedTable.TableName);
                        string[] itemNames = new string[foreignKeyConstraint.Columns.Length];
                        for (int i = 0; i < itemNames.Length; i++)
                        {
                            itemNames[i] = nameMap.GetFieldName(foreignKeyConstraint.Table.TableName, foreignKeyConstraint.Columns[i].ColumnName);
                        }
                        string[] relatedItemNames = new string[foreignKeyConstraint.RelatedColumns.Length];
                        for (int i = 0; i < relatedItemNames.Length; i++)
                        {
                            relatedItemNames[i] = nameMap.GetFieldName(foreignKeyConstraint.RelatedTable.TableName, foreignKeyConstraint.RelatedColumns[i].ColumnName);
                        }

                        XElement relationshipElement = new XElement(Glossary.Relationship);
                        relationshipElement.SetAttributeValue(Glossary.RelationshipFrom, elementName);
                        relationshipElement.SetAttributeValue(Glossary.RelationshipTo, relatedElementName);
                        relationshipElement.SetAttributeValue(Glossary.RelationshipType, "ManyToOne");
                        relationshipElement.SetAttributeValue(Glossary.RelationshipContent, string.Format("{0}({1}),{2}({3})",
                            elementName, itemNames.Aggregate((p, v) => string.Format("{0},{1}", p, v)),
                            relatedElementName, relatedItemNames.Aggregate((p, v) => string.Format("{0},{1}", p, v))
                            ));

                        relationships.Add(relationshipElement);
                    }
                }
            }

            // or
            // DbForeignKey -> Relationship
            if (dataSet.ExtendedProperties.ContainsKey("ForeignKeys"))
            {
                var foreignKeys = dataSet.ExtendedProperties["ForeignKeys"] as IEnumerable<DbForeignKey>;
                foreach (var foreignKey in foreignKeys)
                {
                    string elementName = nameMap.GetElementName(foreignKey.Table);
                    string referencedElementName = nameMap.GetElementName(foreignKey.ReferencedTable);
                    string[] itemNames = new string[foreignKey.Columns.Length];
                    for (int i = 0; i < itemNames.Length; i++)
                    {
                        itemNames[i] = nameMap.GetFieldName(foreignKey.Table, foreignKey.Columns[i]);
                    }
                    string[] referencedItemNames = new string[foreignKey.ReferencedColumns.Length];
                    for (int i = 0; i < referencedItemNames.Length; i++)
                    {
                        referencedItemNames[i] = nameMap.GetFieldName(foreignKey.ReferencedTable, foreignKey.ReferencedColumns[i]);
                    }


                    //
                    XElement relationshipElement = new XElement(Glossary.Relationship);
                    relationshipElement.SetAttributeValue(Glossary.RelationshipFrom, elementName);
                    relationshipElement.SetAttributeValue(Glossary.RelationshipTo, referencedElementName);
                    relationshipElement.SetAttributeValue(Glossary.RelationshipType, "ManyToOne");
                    relationshipElement.SetAttributeValue(Glossary.RelationshipContent, string.Format("{0}({1}),{2}({3})",
                        elementName, itemNames.Aggregate((p, v) => string.Format("{0},{1}", p, v)),
                        referencedElementName, referencedItemNames.Aggregate((p, v) => string.Format("{0},{1}", p, v))
                        ));

                    relationships.Add(relationshipElement);
                }
            }

            XElement schema = new XElement("Schema");

            schema.Add(elements.OrderBy(p => p.Attribute(Glossary.TableType).Value).ThenBy(p => p.Name.LocalName));

            schema.Add(relationships.OrderBy(p => p.Attribute(Glossary.RelationshipFrom).Value).ThenBy(p => p.Attribute(Glossary.RelationshipFrom).Value));

            schema.SetAttributeValue(Glossary.DatabaseKey, dataSet.ExtendedProperties[Glossary.DatabaseKey]);
            schema.SetAttributeValue(Glossary.DatabaseVersion, dataSet.ExtendedProperties[Glossary.DatabaseVersion]);

            if (dataSet.ExtendedProperties.ContainsKey(Glossary.TimezoneOffset))
            {
                schema.SetAttributeValue(Glossary.TimezoneOffset, dataSet.ExtendedProperties[Glossary.TimezoneOffset]);
            }

            schema.SetAttributeValue(Glossary.NameMapVersion, nameMap.NameMapVersion);

            //
            if (dataSet.ExtendedProperties.ContainsKey("DatabaseSequences"))
            {
                DataTable sequencesTable = dataSet.ExtendedProperties["DatabaseSequences"] as DataTable;
                foreach (DataRow row in sequencesTable.Rows)
                {
                    XElement sequenceElement = new XElement("Sequence");
                    sequenceElement.SetAttributeValue("SequenceName", row[0]);
                    schema.Add(sequenceElement);
                }
            }

            return schema;
        }

        private XElement _databaseConfig;
        public XElement DatabaseConfig
        {
            get { return new XElement(_databaseConfig); }
        }

        public void Modify(XElement databaseConfig)
        {
            _databaseConfig = databaseConfig;
            XElement databaseSchema = GetDatabaseSchema();
            databaseSchema.ModifyWithXAttributes(databaseConfig);
        }


    }
}
