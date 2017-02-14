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
        protected class DeleteNodeUnit
        {
            public XElement Node { get; private set; }
            public string XPath { get; private set; }

            public XElement KeySchema { get; set; }
            public XElement TimestampSchema { get; set; }
            public XElement ConcurrencyCheckSchema { get; set; }

            public XElement NullSchema { get; set; }

            public DeleteNodeUnit(XElement node, string xPath)
            {
                Node = node;
                XPath = xPath;
            }
        }

        protected class DeleteUnit : ExecuteUnit
        {
            protected void OnDeleting(DeletingEventArgs args)
            {
                Database.OnDeleting(args);
            }

            protected void OnUpdating(UpdatingEventArgs args)
            {
                Database.OnUpdating(args);
            }

            public DeleteUnit(Database database, XElement element, XElement schema)
                : base(database, element, schema)
            {
            }

            public override void ExecuteNonQuery()
            {
                XElement elementSchema = Schema.GetElementSchema(Element.Name.LocalName);
                XElement keySchema = elementSchema.GetKeySchema();
                XElement timestampSchema = elementSchema.GetTimestampSchema();
                XElement concurrencyCheckSchema = elementSchema.GetConcurrencyCheckSchema();

                //    
                DeleteNodeUnit unit = new DeleteNodeUnit(Element, "/");
                unit.KeySchema = keySchema;
                unit.TimestampSchema = timestampSchema;
                unit.ConcurrencyCheckSchema = concurrencyCheckSchema;

                Stack<DeleteNodeUnit> stack = new Stack<DeleteNodeUnit>();
                stack.Push(unit);

                //
                foreach (XElement children in Element.Elements())
                {
                    if (!Schema.IsSet(children)) continue;

                    FillStack("/" + Element.Name.LocalName, children, Element, stack);
                }

                //
                DeleteNodes(stack);
            }

            // children: Roles, parent: User
            protected void FillStack(string xPath, XElement children, XElement parent, Stack<DeleteNodeUnit> stack)
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

                    //
                    List<DeleteNodeUnit> units = new List<DeleteNodeUnit>();
                    int idx = 0;
                    bool isOnlyOne = children.Elements().Count() == 1;
                    foreach (XElement child in children.Elements())
                    {
                        if (!Schema.IsElement(child)) continue;

                        idx++;
                        string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, idx);

                        //
                        XElement dbChild = FindDbChild(dbChildren, child, mmRelationship.OneToManyRelationship);
                        if (dbChild != null)
                        {
                            dbChild.SetAttributeValue("Found", true.ToString());

                            //
                            if (dbChildTimestampSchema != null) throw new ConcurrencyCheckException(dbChild, null, path, Element, Schema);
                            if (dbChildConcurrencyCheckSchema != null) throw new ConcurrencyCheckException(dbChild, null, path, Element, Schema);

                            //
                            DeleteNodeUnit unit = new DeleteNodeUnit(dbChild, path);
                            unit.NullSchema = nullSchema;
                            unit.KeySchema = dbChildKeySchema;
                            unit.TimestampSchema = null;
                            unit.ConcurrencyCheckSchema = null;
                            units.Add(unit);
                        }
                    }

                    //
                    if (dbChildren.Any(x => x.Attribute("Found") == null))
                        throw new ConcurrencyCheckException(children, null, xPath, Element, Schema);

                    foreach (DeleteNodeUnit unit in units)
                    {
                        stack.Push(unit);
                    }
                }
                else
                {
                    SimpleRelationship simpleRelationship = childrenToParentRelationship as SimpleRelationship;

                    XElement childElementSchema = Schema.GetElementSchemaBySetName(children.Name.LocalName);

                    IEnumerable<DeleteNodeUnit> units = GetDeleteNodeUnits(xPath, children, parent,
                        childElementSchema, simpleRelationship);

                    //
                    foreach (DeleteNodeUnit unit in units)
                    {
                        stack.Push(unit);
                    }

                    //
                    foreach (DeleteNodeUnit unit in units)
                    {
                        foreach (XElement childrenofNode in unit.Node.Elements())
                        {
                            if (!Schema.IsSet(childrenofNode)) continue;

                            FillStack(unit.XPath, childrenofNode, unit.Node, stack);
                        }
                    }
                }
            }

            protected IEnumerable<DeleteNodeUnit> GetDeleteNodeUnits(string xPath, XElement children, XElement parent,
                XElement childElementSchema, SimpleRelationship childrenToParentRelationship)
            {
                Debug.Assert(childrenToParentRelationship is ManyToOneRelationship || childrenToParentRelationship is OneToOneRelationship);

                XElement childKeySchema = childElementSchema.GetKeySchema();
                XElement childTimestampSchema = childElementSchema.GetTimestampSchema();
                XElement childConcurrencyCheckSchema = childElementSchema.GetConcurrencyCheckSchema();

                //
                XElement nullSchema = GetSetNullSchema(childElementSchema, childrenToParentRelationship);

                //
                IEnumerable<XElement> dbChildren = GetChildrenFromDatabase(parent, childrenToParentRelationship);

                //
                List<DeleteNodeUnit> units = new List<DeleteNodeUnit>();
                int idx = 0;
                bool isOnlyOne = children.Elements().Count() == 1;
                foreach (XElement child in children.Elements())
                {
                    if (!Schema.IsElement(child)) continue;

                    idx++;
                    string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, idx);

                    //
                    XElement dbChild = FindDbChild(dbChildren, child, childKeySchema, childTimestampSchema, childConcurrencyCheckSchema, path);

                    //
                    if (dbChild != null)
                    {
                        DeleteNodeUnit unit = new DeleteNodeUnit(child, path);
                        unit.NullSchema = nullSchema;
                        unit.KeySchema = childKeySchema;
                        unit.TimestampSchema = childTimestampSchema;
                        unit.ConcurrencyCheckSchema = childConcurrencyCheckSchema;

                        dbChild.SetAttributeValue("Found", true.ToString());

                        units.Add(unit);
                    }
                }

                //
                if (dbChildren.Any(x => x.Attribute("Found") == null))
                    throw new ConcurrencyCheckException(children, null, xPath, Element, Schema);
                return units;
            }

            //
            internal protected void DeleteNodes(Stack<DeleteNodeUnit> stack)
            {
                int count = stack.Count;
                for (int i = 0; i < count; i++)
                {
                    DeleteNodeUnit unit = stack.Pop();
                    if (unit.NullSchema == null)
                    {
                        DeleteNode(unit);
                    }
                    else
                    {
                        SetNull(unit);
                    }
                }
            }

            protected virtual int SetNull(DeleteNodeUnit unit)
            {
                var args = new UpdatingEventArgs(unit.Node, unit.XPath, Element, Schema);
                OnUpdating(args);

                //
                var command = GetCommand();
                foreach (string sql in args.Before)
                {
                    command.CommandText = sql;
                    int i = command.ExecuteNonQuery();
                }

                //
                XElement whereNode = GetDeleteOrUpdateWhereNode(unit.Node, unit.KeySchema, unit.TimestampSchema, unit.ConcurrencyCheckSchema);
                command.CommandText = Database.GenerateUpdateSql(unit.NullSchema, whereNode, Schema);
                int affected = command.ExecuteNonQuery();
                //if (affected > 1) throw new UnexpectedException(string.Format("UPDATE: {0} rows affected.", affected));
                if (unit.TimestampSchema != null || unit.ConcurrencyCheckSchema != null)
                {
                    if (affected == 0) throw new ConcurrencyCheckException(unit.Node, null, unit.XPath, Element, Schema);
                }

                //
                foreach (string sql in args.After)
                {
                    command.CommandText = sql;
                    int i = command.ExecuteNonQuery();
                }

                //
                Database.PurifyFileds(unit.Node, Schema);

                return affected;
            }

            protected virtual int DeleteNode(DeleteNodeUnit unit)
            {
                var args = new DeletingEventArgs(unit.Node, unit.XPath, Element, Schema);
                OnDeleting(args);

                //
                var command = GetCommand();
                foreach (string sql in args.Before)
                {
                    command.CommandText = sql;
                    int i = command.ExecuteNonQuery();
                }

                //
                XElement whereNode = GetDeleteOrUpdateWhereNode(unit.Node, unit.KeySchema, unit.TimestampSchema, unit.ConcurrencyCheckSchema);
                command.CommandText = Database.GenerateDeleteSql(whereNode, Schema);
                int affected = command.ExecuteNonQuery();
                //if (affected > 1) throw new UnexpectedException(string.Format("DELETE: {0} rows affected.", affected));
                if (unit.TimestampSchema != null || unit.ConcurrencyCheckSchema != null)
                {
                    if (affected == 0) throw new ConcurrencyCheckException(unit.Node, null, unit.XPath, Element, Schema);
                }

                //
                Database.PurifyFileds(unit.Node, Schema);

                return affected;
            }


        }
    }
}
