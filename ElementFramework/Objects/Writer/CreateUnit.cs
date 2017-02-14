using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public abstract partial class Database
    {
        protected class CreateUnit : ExecuteUnit
        {
            protected void OnInserting(InsertingEventArgs args)
            {
                Database.OnInserting(args);
            }

            protected void OnInserted(InsertedEventArgs args)
            {
                Database.OnInserted(args);
            }

            protected void SetDefaultValues(XElement node)
            {
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                foreach (XElement fieldSchema in elementSchema.Elements().Where(p => p.Element("DefaultValue") != null))
                {
                    if (node.Element(fieldSchema.Name) != null && node.Element(fieldSchema.Name).Value != string.Empty)
                    {
                        continue;
                    }
                    DefaultValueAttribute attribute = fieldSchema.Element("DefaultValue").CreateDefaultValueAttribute();
                    if (attribute.Value.ToString() == "DateTime.Now")
                    {
                        node.SetElementValue(fieldSchema.Name, Database.GetNow().ToCanonicalString());
                    }
                    else if (attribute.Value.ToString() == "DateTime.UtcNow")
                    {
                        node.SetElementValue(fieldSchema.Name, Database.GetUtcNow().ToCanonicalString());
                    }
                    else
                    {
                        node.SetElementValue(fieldSchema.Name, attribute.Value);
                    }
                }
            }

            protected void SetSequenceValue(XElement node)
            {
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                XElement sequenceFieldSchema = elementSchema.Elements().FirstOrDefault(p => p.Element(Glossary.Sequence) != null);
                if (sequenceFieldSchema == null) return;

                // AutoIncrement first
                XAttribute attr = sequenceFieldSchema.Attribute("AutoIncrement");

                // autoIncrement
                if (attr != null && attr.Value == true.ToString()) return;


                // caller provided second
                XElement field = node.Element(sequenceFieldSchema.Name);

                // caller has not provided a value for sequence field.
                if (field != null && !string.IsNullOrWhiteSpace(field.Value)) return;

                // GenerateSequence third
                SequenceAttribute attribute = sequenceFieldSchema.Element(Glossary.Sequence).CreateSequenceAttribute();
                node.SetElementValue(sequenceFieldSchema.Name, Database.GenerateSequence(attribute.SequenceName).ToString());
                if (node.Elements().Any(x => x.Attribute("DataType") != null))
                {
                    node.Element(sequenceFieldSchema.Name).SetAttributeValue("DataType", sequenceFieldSchema.Attribute("DataType").Value);
                }
            }

            public CreateUnit(Database database, XElement element, XElement schema)
                : base(database, element, schema)
            {
            }

            public override void ExecuteNonQuery()
            {
                Execute("/" + Element.Name.LocalName, Element);
            }

            // Update without Original, Update with Original
            internal protected virtual void ExecuteNonQuery(string baseXPath)
            {
                Execute(baseXPath, Element);
            }

            protected void Execute(string xPath, XElement node, SimpleRelationship relationship = null, XElement related = null)
            {
                if (relationship != null)
                {
                    Debug.Assert(relationship is ManyToOneRelationship || relationship is OneToOneRelationship);

                    for (int i = 0; i < relationship.FieldNames.Length; i++)
                    {
                        node.SetElementValue(relationship.FieldNames[i], related.Element(relationship.RelatedFieldNames[i]).Value);
                    }
                }

                //
                InsertNode(xPath, node);

                foreach (XElement children in node.Elements())
                {
                    if (!Schema.IsSet(children)) continue;

                    Relationship childRelationship = GetChildrenToParentRelationship(children.Name.LocalName, node.Name.LocalName);

                    int idx = 0;
                    bool isOnlyOne = children.Elements().Count() == 1;
                    foreach (XElement child in children.Elements())
                    {
                        if (!Schema.IsElement(child)) continue;

                        idx++;
                        string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, idx);

                        if (childRelationship is ManyToManyRelationship)
                        {
                            ManyToManyRelationship mmRelationship = childRelationship as ManyToManyRelationship;
                            XElement mmChild = mmRelationship.CreateManyToManyElement(child, node);
                            InsertNode(path, mmChild);
                        }
                        else
                        {
                            Execute(path, child, childRelationship as SimpleRelationship, node);
                        }
                    }
                }
            }

            protected virtual int InsertNode(string xPath, XElement node)
            {
                SetFromInternalParents(xPath, node);

                //
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                XElement keySchema = elementSchema.GetKeySchema();
                XElement autoFieldSchema = keySchema.Elements().FirstOrDefault(p => p.Attribute("AutoIncrement") != null);

                SetDefaultValues(node);
                SetSequenceValue(node);

                InsertingEventArgs insertingEventArgs = new InsertingEventArgs(node, xPath, Element, Schema);
                OnInserting(insertingEventArgs);

                DbCommand command = GetCommand();
                command.Parameters.Clear();
                DbParameter[] parameter;
                command.CommandText = Database.GenerateInsertSql(node, Schema, out parameter);
                command.Parameters.AddRange(parameter);
                int affected;
                try
                {
                    affected = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    throw e;
                }
                //if (affected != 1) throw new UnexpectedException(string.Format("INSERT: {0} rows affected.", affected));
                if (autoFieldSchema != null)
                {
                    // SQL Server
                    command.CommandText = "SELECT SCOPE_IDENTITY();";
                    object obj = command.ExecuteScalar();
                    node.SetElementValue(autoFieldSchema.Name, obj.ToString());
                    if (node.Elements().Any(x => x.Attribute("DataType") != null))
                    {
                        node.Element(autoFieldSchema.Name).SetAttributeValue("DataType", autoFieldSchema.Attribute("DataType").Value);
                    }
                }

                var args = new InsertedEventArgs(node, xPath, Element, Schema);
                OnInserted(args);

                //
                foreach (var sql in args.After)
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }

                //
                Database.PurifyFileds(node, Schema);

                return affected;
            }


        }
    }
}
