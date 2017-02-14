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
        protected class UpdateNodeUnit
        {
            public XElement Node { get; private set; }
            public string XPath { get; private set; }

            public XElement ElementSchema { get; set; } // null: Create
            public XElement KeySchema { get; set; }
            public XElement TimestampSchema { get; set; }
            public XElement ConcurrencyCheckSchema { get; set; }

            public UpdateNodeUnit(XElement node, string xPath)
            {
                Node = node;
                XPath = xPath;
            }
        }

        protected partial class UpdateUnit : ExecuteUnit
        {
            protected void OnUpdating(UpdatingEventArgs args)
            {
                Database.OnUpdating(args);
            }

            protected ICreateUnitFactory CreateUnitFactory;
            protected IDeleteUnitFactory DeleteUnitFactory;

            public UpdateUnit(Database database, XElement element, XElement schema,
                  ICreateUnitFactory createUnitFactory, DeleteUnitFactory deleteUnitFactory)
                : base(database, element, schema)
            {
                CreateUnitFactory = createUnitFactory;
                DeleteUnitFactory = deleteUnitFactory;
            }

            public override void ExecuteNonQuery()
            {
                if (Original == null)
                {
                    Execute();
                }
                else
                {
                    ExecuteWithOriginal();
                }
            }

            protected void Execute()
            {
                XElement elementSchema = Schema.GetElementSchema(Element.Name.LocalName);
                XElement keySchema = elementSchema.GetKeySchema();
                XElement timestampSchema = elementSchema.GetTimestampSchema();
                XElement concurrencyCheckSchema = elementSchema.GetConcurrencyCheckSchema();

                //    
                UpdateNodeUnit unit = new UpdateNodeUnit(Element, "/");
                unit.ElementSchema = elementSchema;
                unit.KeySchema = keySchema;
                unit.TimestampSchema = timestampSchema;
                unit.ConcurrencyCheckSchema = concurrencyCheckSchema;

                //
                Queue<UpdateNodeUnit> queue = new Queue<UpdateNodeUnit>();
                queue.Enqueue(unit);

                Stack<DeleteNodeUnit> stack = new Stack<DeleteNodeUnit>();

                //
                foreach (XElement children in Element.Elements())
                {
                    if (!Schema.IsSet(children)) continue;

                    FillQueue("/" + Element.Name.LocalName, children, Element, queue, stack);
                }

                //
                CreateOrUpdate(queue);

                //
                DeleteUnit deleteUnit = DeleteUnitFactory.Create(Database, Element, Schema);
                deleteUnit.DeleteNodes(stack);
            }

            protected void FillQueue(string xPath, XElement children, XElement parent, Queue<UpdateNodeUnit> queue, Stack<DeleteNodeUnit> stack)
            {
                Relationship childrenToParentRelationship = GetChildrenToParentRelationship(children.Name.LocalName, parent.Name.LocalName);
                if (childrenToParentRelationship is ManyToManyRelationship)
                {
                    ManyToManyRelationship mmRelationship = childrenToParentRelationship as ManyToManyRelationship;

                    // UserRole
                    XElement dbChildElementSchema = Schema.GetElementSchema(mmRelationship.ManyToOneRelationship.ElementName);
                    XElement dbChildKeySchema = dbChildElementSchema.GetKeySchema();
                    XElement dbChildTimestampSchema = dbChildElementSchema.GetTimestampSchema();
                    XElement dbChildConcurrencyCheckSchema = dbChildElementSchema.GetConcurrencyCheckSchema();

                    //
                    XElement nullSchema = GetSetNullSchema(dbChildElementSchema, mmRelationship.OneToManyRelationship);

                    // UserRoles
                    IEnumerable<XElement> dbChildren = GetChildrenFromDatabase(parent, childrenToParentRelationship as ManyToManyRelationship);

                    // insert
                    int idx = 0;
                    bool isOnlyOne = children.Elements().Count() == 1;
                    foreach (XElement child in children.Elements())
                    {
                        if (!Schema.IsElement(child)) continue;

                        idx++;
                        string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, idx);

                        //
                        XElement dbChild = FindDbChild(dbChildren, child, mmRelationship.OneToManyRelationship);

                        XElement mmChild = mmRelationship.CreateManyToManyElement(child, parent);
                        if (dbChild == null)
                        {
                            UpdateNodeUnit unit = new UpdateNodeUnit(mmChild, xPath);
                            unit.ElementSchema = null;
                            queue.Enqueue(unit);
                        }
                        else
                        {
                            dbChild.SetAttributeValue("Found", true.ToString());
                        }
                    }

                    // delete
                    IEnumerable<XElement> notFounds = dbChildren.Where(x => x.Attribute("Found") == null);
                    foreach (XElement notFound in notFounds)
                    {
                        if (dbChildTimestampSchema != null || dbChildConcurrencyCheckSchema != null)
                        {
                            throw new ConcurrencyCheckException(notFound, null, xPath, Element, Schema);
                        }

                        //
                        DeleteNodeUnit unit = new DeleteNodeUnit(notFound, xPath);
                        unit.NullSchema = nullSchema;
                        unit.KeySchema = dbChildKeySchema;
                        unit.TimestampSchema = null;
                        unit.ConcurrencyCheckSchema = null;

                        stack.Push(unit);
                    }
                }
                else
                {
                    XElement childElementSchema = Schema.GetElementSchemaBySetName(children.Name.LocalName);

                    IEnumerable<DeleteNodeUnit> deleteNodeUnits;
                    IEnumerable<UpdateNodeUnit> units = GetUpdateNodeUnits(xPath, children, parent,
                        childElementSchema, childrenToParentRelationship as SimpleRelationship, out deleteNodeUnits);

                    //
                    foreach (UpdateNodeUnit unit in units)
                    {
                        queue.Enqueue(unit);
                    }

                    //
                    foreach (DeleteNodeUnit unit in deleteNodeUnits)
                    {
                        stack.Push(unit);
                    }

                    //
                    foreach (UpdateNodeUnit unit in units.Where(u => u.ElementSchema != null))
                    {
                        foreach (XElement childrenofNode in unit.Node.Elements())
                        {
                            if (!Schema.IsSet(childrenofNode)) continue;

                            //
                            FillQueue(unit.XPath, childrenofNode, unit.Node, queue, stack);
                        }
                    }
                }
            }

            protected IEnumerable<UpdateNodeUnit> GetUpdateNodeUnits(string xPath, XElement children, XElement parent,
                XElement childElementSchema, SimpleRelationship childrenToParentRelationship,
                out IEnumerable<DeleteNodeUnit> deleteNodeUnits)
            {
                List<DeleteNodeUnit> deleteList = new List<DeleteNodeUnit>();

                XElement childKeySchema = childElementSchema.GetKeySchema();
                XElement childTimestampSchema = childElementSchema.GetTimestampSchema();
                XElement childConcurrencyCheckSchema = childElementSchema.GetConcurrencyCheckSchema();

                //
                XElement nullSchema = GetSetNullSchema(childElementSchema, childrenToParentRelationship);

                //
                IEnumerable<XElement> dbChildren = GetChildrenFromDatabase(parent, childrenToParentRelationship as SimpleRelationship);

                //
                List<UpdateNodeUnit> units = new List<UpdateNodeUnit>();
                int idx = 0;
                bool isOnlyOne = children.Elements().Count() == 1;
                foreach (XElement child in children.Elements())
                {
                    if (!Schema.IsElement(child)) continue;

                    idx++;
                    string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, idx);

                    //
                    SetFromInternalParents(path, child);

                    //
                    XElement dbChild = FindDbChild(dbChildren, child, childKeySchema, childTimestampSchema, childConcurrencyCheckSchema, path);
                    UpdateNodeUnit unit = new UpdateNodeUnit(child, path);

                    if (dbChild == null)
                    {
                        unit.ElementSchema = null;
                    }
                    else
                    {
                        dbChild.SetAttributeValue("Found", true.ToString());

                        unit.ElementSchema = childElementSchema;
                        unit.KeySchema = childKeySchema;
                        unit.TimestampSchema = childTimestampSchema;
                        unit.ConcurrencyCheckSchema = childConcurrencyCheckSchema;
                    }
                    units.Add(unit);
                }

                //
                IEnumerable<XElement> notFounds = dbChildren.Where(x => x.Attribute("Found") == null);
                foreach (XElement notFound in notFounds)
                {
                    if (childTimestampSchema != null || childConcurrencyCheckSchema != null)
                    {
                        throw new ConcurrencyCheckException(notFound, null, xPath, Element, Schema);
                    }

                    DeleteNodeUnit unit = new DeleteNodeUnit(notFound, xPath);
                    unit.NullSchema = nullSchema;
                    unit.KeySchema = childKeySchema;
                    unit.TimestampSchema = childTimestampSchema;
                    unit.ConcurrencyCheckSchema = childConcurrencyCheckSchema;

                    deleteList.Add(unit);
                }

                deleteNodeUnits = deleteList;
                return units;
            }

            protected XElement GetUpdateSetNode(XElement node, XElement elementSchema, XElement keySchema, XElement timestampSchema)
            {
                XElement updateSetNode = new XElement(node);

                foreach (XElement fieldSchema in keySchema.Elements())
                {
                    updateSetNode.Element(fieldSchema.Name).Remove();
                }

                IEnumerable<XElement> readOnlyFields = elementSchema.Elements()
                    .Where(x => x.Attribute("ReadOnly") != null && x.Attribute("ReadOnly").Value == true.ToString());
                foreach (XElement fieldSchema in readOnlyFields)
                {
                    updateSetNode.Element(fieldSchema.Name).Remove();
                }

                if (timestampSchema != null)
                {
                    updateSetNode.Element(timestampSchema.Name).Remove();
                }
                return updateSetNode;
            }

            protected void CreateOrUpdate(Queue<UpdateNodeUnit> queue)
            {
                int count = queue.Count;
                for (int i = 0; i < count; i++)
                {
                    UpdateNodeUnit unit = queue.Dequeue();
                    if (unit.ElementSchema == null)
                    {
                        CreateUnit createUnit = CreateUnitFactory.Create(Database, unit.Node, Schema);
                        createUnit.ExecuteNonQuery(unit.XPath);
                    }
                    else
                    {
                        UpdateNode(unit);
                    }
                }
            }

            protected int UpdateNode(UpdateNodeUnit unit)
            {
                var args = new UpdatingEventArgs(unit.Node, unit.XPath, Element, Schema);
                OnUpdating(args);

                //
                var command = GetCommand();
                foreach (string sql in args.Before)
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }

                //
                XElement updateSetNode = GetUpdateSetNode(unit.Node, unit.ElementSchema, unit.KeySchema, unit.TimestampSchema);
                XElement whereNode = GetDeleteOrUpdateWhereNode(unit.Node, unit.KeySchema, unit.TimestampSchema, unit.ConcurrencyCheckSchema);
                string updateSql = Database.GenerateUpdateSql(updateSetNode, whereNode, Schema);

                int affected = 0;
                if (!string.IsNullOrWhiteSpace(updateSql))
                {
                    command.CommandText = updateSql;
                    affected = command.ExecuteNonQuery();
                    //if (affected != 1) throw new UnexpectedException(string.Format("UPDATE: {0} rows affected.", affected));
                    //if (affected > 1) throw new UnexpectedException(string.Format("UPDATE: {0} rows affected.", affected));
                    if (unit.TimestampSchema != null || unit.ConcurrencyCheckSchema != null)
                    {
                        if (affected == 0) throw new ConcurrencyCheckException(unit.Node, null, unit.XPath, Element, Schema);
                    }
                }

                //
                foreach (string sql in args.After)
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }

                //
                Database.PurifyFileds(unit.Node, Schema);

                return affected;
            }


        }
    }
}
