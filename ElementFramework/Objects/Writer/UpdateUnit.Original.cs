using System;
using System.Collections.Generic;
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
        protected partial class UpdateUnit : ExecuteUnit
        {
            protected readonly XElement Original;

            public UpdateUnit(Database database, XElement element, XElement original, XElement schema,
                  ICreateUnitFactory createUnitFactory, DeleteUnitFactory deleteUnitFactory)
                : base(database, element, schema)
            {
                Debug.Assert(original != null);
                Debug.Assert(element.Name == original.Name);

                Original = new XElement(original);
                CreateUnitFactory = createUnitFactory;
                DeleteUnitFactory = deleteUnitFactory;
            }

            protected void ExecuteWithOriginal()
            {
                InsertOrUpdate("/" + Element.Name.LocalName, Element, Original);

                //
                Stack<DeleteNodeUnit> stack = new Stack<DeleteNodeUnit>();
                FillDeleteStack("/" + Original.Name.LocalName, Original, stack);

                //
                DeleteUnit deleteUnit = DeleteUnitFactory.Create(Database, Element, Schema);
                deleteUnit.DeleteNodes(stack);
            }

            protected void InsertOrUpdate(string xPath, XElement node, XElement origNode)
            {
                UpdateNode(xPath, node);

                foreach (XElement children in node.Elements())
                {
                    if (!Schema.IsSet(children)) continue;

                    //
                    XElement origChildren = origNode.Element(children.Name);
                    if (origChildren == null) origChildren = new XElement(children.Name);

                    //
                    Relationship childrenToParentRelationship = GetChildrenToParentRelationship(children.Name.LocalName, node.Name.LocalName);

                    //
                    int idx = 0;
                    bool isOnlyOne = children.Elements().Count() == 1;
                    foreach (XElement child in children.Elements())
                    {
                        if (!Schema.IsElement(child)) continue;

                        idx++;
                        string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, idx);

                        //
                        XElement origChild = FindOrigChild(origChildren.Elements(), child);

                        if (childrenToParentRelationship is ManyToManyRelationship)
                        {
                            ManyToManyRelationship mmRelationship = childrenToParentRelationship as ManyToManyRelationship;

                            XElement mmChild = mmRelationship.CreateManyToManyElement(child, node);
                            if (origChild == null)
                            {
                                CreateUnit createUnit = CreateUnitFactory.Create(Database, mmChild, Schema);
                                createUnit.ExecuteNonQuery(path);
                            }
                            else
                            {
                                origChild.SetAttributeValue("Found", true.ToString());
                                UpdateNode(path, mmChild);
                            }
                        }
                        else
                        {
                            SimpleRelationship simpleRelationship = childrenToParentRelationship as SimpleRelationship;

                            for (int i = 0; i < simpleRelationship.FieldNames.Length; i++)
                            {
                                child.SetElementValue(simpleRelationship.FieldNames[i], node.Element(simpleRelationship.RelatedFieldNames[i]).Value);
                            }
                            if (origChild == null)
                            {
                                CreateUnit createUnit = CreateUnitFactory.Create(Database, child, Schema);
                                createUnit.ExecuteNonQuery(path);
                            }
                            else
                            {
                                origChild.SetAttributeValue("Found", true.ToString());

                                //
                                InsertOrUpdate(path, child, origChild);
                            }
                        }
                    }
                }
            }

            protected XElement FindOrigChild(IEnumerable<XElement> origChildren, XElement child)
            {
                XElement childKey = Schema.GetElementSchema(child.Name.LocalName).GetKeySchema().ExtractKey(child);

                IEnumerable<XElement> origs = origChildren.Filter(childKey);
                int count = origs.Count();
                if (count == 0) return null;
                if (count == 1) return origs.First();

                throw new UnexpectedException(string.Format(Messages.Duplicate_Original_Found, childKey.ToString()));
            }

            protected void FillDeleteStack(string xPath, XElement origNode, Stack<DeleteNodeUnit> stack)
            {
                foreach (XElement children in origNode.Elements())
                {
                    if (!Schema.IsSet(children)) continue;

                    //
                    Relationship childrenToParentRelationship = GetChildrenToParentRelationship(children.Name.LocalName, origNode.Name.LocalName);

                    //
                    if (childrenToParentRelationship is ManyToManyRelationship)
                    {
                        ManyToManyRelationship mmRelationship = childrenToParentRelationship as ManyToManyRelationship;

                        //
                        XElement dbChildElementSchema = Schema.GetElementSchema(mmRelationship.ManyToOneRelationship.ElementName);
                        XElement dbChildKeySchema = dbChildElementSchema.GetKeySchema();
                        XElement dbChildTimestampSchema = dbChildElementSchema.GetTimestampSchema();
                        XElement dbChildConcurrencyCheckSchema = dbChildElementSchema.GetConcurrencyCheckSchema();

                        //
                        XElement nullSchema = GetSetNullSchema(dbChildElementSchema, mmRelationship.OneToManyRelationship);

                        //
                        int idx = 0;
                        bool isOnlyOne = children.Elements().Count() == 1;
                        foreach (XElement child in children.Elements())
                        {
                            if (!Schema.IsElement(child)) continue;

                            idx++;
                            string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, idx);

                            if (child.Attribute("Found") == null)
                            {
                                XElement mmChild = mmRelationship.CreateManyToManyElement(child, origNode);

                                DeleteNodeUnit unit = new DeleteNodeUnit(mmChild, path);
                                unit.NullSchema = nullSchema;
                                unit.KeySchema = dbChildKeySchema;
                                unit.TimestampSchema = dbChildTimestampSchema;
                                unit.ConcurrencyCheckSchema = dbChildConcurrencyCheckSchema;
                                stack.Push(unit);
                            }
                            else
                            {
                                child.RemoveAttributes();
                            }
                        }
                    }
                    else
                    {
                        SimpleRelationship simpleRelationship = childrenToParentRelationship as SimpleRelationship;

                        XElement childElementSchema = Schema.GetElementSchemaBySetName(children.Name.LocalName);
                        XElement childKeySchema = childElementSchema.GetKeySchema();
                        XElement childTimestampSchema = childElementSchema.GetTimestampSchema();
                        XElement childConcurrencyCheckSchema = childElementSchema.GetConcurrencyCheckSchema();

                        //
                        XElement nullSchema = GetSetNullSchema(childElementSchema, simpleRelationship);

                        //
                        int idx = 0;
                        bool isOnlyOne = children.Elements().Count() == 1;
                        foreach (XElement child in children.Elements())
                        {
                            if (!Schema.IsElement(child)) continue;

                            idx++;
                            string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, idx);

                            if (child.Attribute("Found") == null)
                            {
                                DeleteNodeUnit unit = new DeleteNodeUnit(child, path);
                                unit.NullSchema = nullSchema;
                                unit.KeySchema = childKeySchema;
                                unit.TimestampSchema = childTimestampSchema;
                                unit.ConcurrencyCheckSchema = childConcurrencyCheckSchema;

                                stack.Push(unit);
                            }
                            else
                            {
                                child.RemoveAttributes();

                                //
                                FillDeleteStack(path, child, stack);
                            }
                        }
                    }
                }
            }

            protected int UpdateNode(string xPath, XElement node)
            {
                SetFromInternalParents(xPath, node);

                //                
                var args = new UpdatingEventArgs(node, xPath, Element, Schema);
                OnUpdating(args);

                //
                var command = GetCommand();
                foreach (string sql in args.Before)
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }

                //
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                XElement keySchema = elementSchema.GetKeySchema();
                XElement timestampSchema = elementSchema.GetTimestampSchema();
                XElement concurrencyCheckSchema = elementSchema.GetConcurrencyCheckSchema();

                //
                XElement updateSetNode = GetUpdateSetNode(node, elementSchema, keySchema, timestampSchema);
                XElement whereNode = GetDeleteOrUpdateWhereNode(node, keySchema, timestampSchema, concurrencyCheckSchema);
                command.CommandText = Database.GenerateUpdateSql(updateSetNode, whereNode, Schema);
                int affected = command.ExecuteNonQuery();
                //if (affected != 1) throw new UnexpectedException(string.Format("UPDATE: {0} rows affected.", affected));
                //if (affected > 1) throw new UnexpectedException(string.Format("UPDATE: {0} rows affected.", affected));
                if (timestampSchema != null || concurrencyCheckSchema != null)
                {
                    if (affected == 0) throw new ConcurrencyCheckException(node, null, xPath, Element, Schema);
                }

                //
                foreach (string sql in args.After)
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
