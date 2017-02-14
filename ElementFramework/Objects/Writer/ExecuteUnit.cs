using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Schema;
using XData.Data.Resources;
using System.Diagnostics;
using System.ComponentModel;

namespace XData.Data.Objects
{
    public abstract partial class Database
    {
        // is a Element
        protected abstract class ExecuteUnit
        {
            protected readonly Database Database;
            protected readonly XElement Element;
            protected readonly XElement Schema;

            public ExecuteUnit(Database database, XElement element, XElement schema)
            {
                Database = database;
                Element = element;
                Schema = schema;
            }

            public abstract void ExecuteNonQuery();

            private DbCommand _command = null;
            protected DbCommand GetCommand()
            {
                if (_command == null)
                {
                    _command = Database.Connection.CreateCommand();
                    if (Database.Transaction != null)
                    {
                        _command.Transaction = Database.Transaction;
                    }
                }
                return _command;
            }

            protected Relationship GetChildrenToParentRelationship(string childrenName, string parentName)
            {
                XElement childElementSchema = Schema.GetElementSchemaBySetName(childrenName);
                var childRelationship = Schema.CreateRelationship(childElementSchema.Name.LocalName, parentName);
                if (childRelationship == null || childRelationship is OneToManyRelationship)
                {
                    throw new SchemaException(string.Format(Messages.Validation_No_Available_Relationship, childrenName, parentName),
                        Schema);
                }
                return childRelationship;
            }

            // CreateUnit, UpdateUnit
            // <User>
            //  <Pserson> // inversion
            //      <Id>123</Id>
            //      ...
            //  </Person>
            // </User>
            protected void SetFromInternalParents(string xPath, XElement node)
            {
                foreach (XElement parent in node.Elements().Where(p => p.HasElements))
                {
                    if (!Schema.IsElement(parent)) continue;
                    SetFromInternalParent(xPath, node, parent);
                }
            }

            private void SetFromInternalParent(string xPath, XElement node, XElement parent)
            {
                Relationship relationship = Schema.CreateToOneRelationship(node.Name.LocalName, parent.Name.LocalName);
                if (relationship == null) return;
                if (relationship is ManyToOneRelationship || relationship is OneToOneRelationship)
                {
                    SimpleRelationship simpleRelationship = relationship as SimpleRelationship;
                    for (int i = 0; i < simpleRelationship.FieldNames.Length; i++)
                    {
                        string name = simpleRelationship.FieldNames[i];
                        string value = parent.Element(simpleRelationship.RelatedFieldNames[i]).Value;
                        if (node.Element(name) == null || string.IsNullOrWhiteSpace(node.Element(name).Value))
                        {
                            node.SetElementValue(name, value);
                        }
                        else
                        {
                            Debug.Assert(node.Element(name).Value == value);
                        }
                    }
                }
            }

            // Delete, Update without Original
            protected IEnumerable<XElement> GetChildrenFromDatabase(XElement parent, ManyToManyRelationship childrenToParentRelationship)
            {
                var manyToOneRelationship = childrenToParentRelationship.ManyToOneRelationship;
                XElement dbChild = new XElement(manyToOneRelationship.ElementName);
                for (int i = 0; i < manyToOneRelationship.FieldNames.Length; i++)
                {
                    dbChild.SetElementValue(manyToOneRelationship.FieldNames[i],
                      parent.Element(manyToOneRelationship.RelatedFieldNames[i]).Value);
                }
                return Database.GetDatabaseElements(dbChild, Schema);
            }

            // Delete, Update without Original
            protected IEnumerable<XElement> GetChildrenFromDatabase(XElement parent, SimpleRelationship childrenToParentRelationship)
            {
                Debug.Assert(childrenToParentRelationship is ManyToOneRelationship || childrenToParentRelationship is OneToOneRelationship);

                XElement dbChild = new XElement(childrenToParentRelationship.ElementName);
                for (int i = 0; i < childrenToParentRelationship.FieldNames.Length; i++)
                {
                    dbChild.SetElementValue(childrenToParentRelationship.FieldNames[i],
                        parent.Element(childrenToParentRelationship.RelatedFieldNames[i]).Value);
                }
                return Database.GetDatabaseElements(dbChild, Schema);
            }

            // Delete, Update without Original
            protected XElement GetDeleteOrUpdateWhereNode(XElement node, XElement keySchema, XElement timestampSchema, XElement concurrencyCheckSchema)
            {
                XElement whereNode = keySchema.ExtractKey(node);
                if (timestampSchema != null)
                {
                    XElement timestamp = timestampSchema.ExtractTimestamp(node);
                    whereNode.SetElementValue(timestamp.Name, timestamp.Value);
                }
                if (concurrencyCheckSchema != null)
                {
                    XElement checksElement = concurrencyCheckSchema.ExtractConcurrencyChecks(node);
                    whereNode.Add(checksElement.Elements());
                }
                return whereNode;
            }

            // Delete, Update without Original
            protected XElement FindDbChild(IEnumerable<XElement> dbChildren, XElement child,
                XElement childKeySchema, XElement childTimestampSchema, XElement childConcurrencyCheckSchema, string xPath)
            {
                // 
                XElement timestamp = null;
                if (childTimestampSchema != null)
                {
                    timestamp = childTimestampSchema.ExtractTimestamp(child);
                    if (timestamp == null)
                        throw new ConcurrencyCheckException(child, null, xPath, Element, Schema);
                }

                //
                XElement concurrencyChecks = null;
                if (childConcurrencyCheckSchema != null)
                {
                    concurrencyChecks = childConcurrencyCheckSchema.ExtractConcurrencyChecks(child);
                    if (concurrencyChecks == null)
                        throw new ConcurrencyCheckException(child, null, xPath, Element, Schema);
                }

                //
                XElement value = childKeySchema.ExtractKey(child);
                IEnumerable<XElement> result = dbChildren.Filter(value);
                if (result.Count() > 1) throw new UnexpectedException("Duplicated.");
                XElement dbChild = (result.Count() == 0) ? null : result.First();

                if (dbChild != null)
                {
                    //
                    if (childTimestampSchema != null)
                    {
                        if (childTimestampSchema.ExtractTimestamp(dbChild) != timestamp)
                            throw new ConcurrencyCheckException(child, null, xPath, Element, Schema);
                    }

                    //
                    if (childConcurrencyCheckSchema != null)
                    {
                        if (!XNode.DeepEquals(childConcurrencyCheckSchema.ExtractConcurrencyChecks(dbChild), concurrencyChecks))
                            throw new ConcurrencyCheckException(child, null, xPath, Element, Schema);
                    }
                }
                return dbChild;
            }

            // Delete, Update without Original
            protected XElement GetSetNullSchema(XElement childElementSchema, SimpleRelationship childrenToParentRelationship)
            {
                Debug.Assert(childrenToParentRelationship is ManyToOneRelationship || childrenToParentRelationship is OneToOneRelationship);

                XElement nullSchema = new XElement(childElementSchema.Name);
                for (int i = 0; i < childrenToParentRelationship.FieldNames.Length; i++)
                {
                    XElement fieldSchema = childElementSchema.Element(childrenToParentRelationship.FieldNames[i]);
                    if (fieldSchema.Element("Required") == null)
                    {
                        nullSchema.Add(new XElement(fieldSchema.Name));
                        continue;
                    }
                    nullSchema = null;
                    break;
                }
                return nullSchema;
            }

            // Delete, Update without Original
            // ManyToMany
            protected XElement GetSetNullSchema(XElement dbChildElementSchema, OneToManyRelationship relationship)
            {
                XElement nullSchema = new XElement(dbChildElementSchema.Name);
                for (int i = 0; i < relationship.FieldNames.Length; i++)
                {
                    XElement fieldSchema = dbChildElementSchema.Element(relationship.RelatedFieldNames[i]);
                    if (fieldSchema.Attribute("AllowDBNull").Value == "True" && fieldSchema.Element("Required") == null)
                    {
                        nullSchema.Add(new XElement(fieldSchema.Name));
                    }
                    else
                    {
                        nullSchema = new XElement(dbChildElementSchema.Name);
                        break;
                    }
                }
                return nullSchema.HasElements ? nullSchema : null;
            }

            // Delete, Update without Original
            // ManyToMany
            // UserRoles, Role
            protected XElement FindDbChild(IEnumerable<XElement> dbChildren, XElement child, OneToManyRelationship relationship)
            {
                XElement value = new XElement(relationship.RelatedElementName);
                for (int i = 0; i < relationship.RelatedFieldNames.Length; i++)
                {
                    value.SetElementValue(relationship.RelatedFieldNames[i],
                        child.Element(relationship.FieldNames[i]).Value);
                }
                IEnumerable<XElement> result = dbChildren.Filter(value);
                if (result.Count() > 1) throw new UnexpectedException("Duplicated.");
                XElement dbChild = (result.Count() == 0) ? null : result.First();
                return dbChild;
            }


        }
    }
}
