using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Schema;
using XData.Data.Resources;
using System.Diagnostics;
using System.Data.Common;

namespace XData.Data.Objects
{
    public abstract partial class Database
    {
        public event InsertingEventHandler Inserting;
        public event InsertedEventHandler Inserted;
        public event DeletingEventHandler Deleting;
        public event UpdatingEventHandler Updating;

        internal void OnInserting(InsertingEventArgs args)
        {
            if (Inserting != null)
            {
                Inserting(this, args);
            }
        }

        internal void OnInserted(InsertedEventArgs args)
        {
            if (Inserted != null)
            {
                Inserted(this, args);
            }
        }

        internal void OnDeleting(DeletingEventArgs args)
        {
            if (Deleting != null)
            {
                Deleting(this, args);
            }
        }

        internal void OnUpdating(UpdatingEventArgs args)
        {
            if (Updating != null)
            {
                Updating(this, args);
            }
        }

        internal protected abstract object GenerateSequence(string sequenceName);

        internal protected virtual IEnumerable<XElement> GetDatabaseElements(XElement whereNode, XElement schema)
        {
            string sql = GenerateGetDatabaseElementsSql(whereNode, schema);
            IEnumerable<XElement> elements = SqlQuery(whereNode.Name.LocalName, sql);
            return elements;
        }

        protected virtual string GenerateGetDatabaseElementsSql(XElement whereNode, XElement schema)
        {
            XElement row = NodeToRow(whereNode, schema);
            IEnumerable<string> whereFragments = row.Elements().Select(p => string.Format("{0} = {1}", DecorateColumnName(p.Name.LocalName), p.Value));
            string where = string.Join(" AND ", whereFragments);
            string sql = string.Format("SELECT * FROM {0} WHERE {1}", DecorateTableName(row.Name.LocalName), where);
            return sql;
        }

        protected virtual string GenerateInsertSql(XElement node, XElement schema, out DbParameter[] parameters)
        {
            XElement row = NodeToRow(node, schema);

            var rowElements = row.Elements().Where(x => x.Value != "NULL");
            string sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", DecorateTableName(row.Name.LocalName),
                string.Join(",", rowElements.Select(x => DecorateColumnName(x.Name.LocalName))),
                string.Join(",", rowElements.Select(x => x.Value)));
            parameters = new DbParameter[0];
            return sql;
        }

        protected virtual string GenerateDeleteSql(XElement whereNode, XElement schema)
        {
            string where = GenerateDeleteOrUpdateWhere(whereNode, schema);
            string table = schema.GetElementSchema(whereNode.Name.LocalName).Attribute(Glossary.Table).Value;
            string sql = string.Format("DELETE FROM {0} WHERE {1}", DecorateTableName(table), where);
            return sql;
        }

        protected string GenerateDeleteOrUpdateWhere(XElement whereNode, XElement schema)
        {
            XElement whereRow = NodeToRow(whereNode, schema);
            XElement elementSchema = schema.GetElementSchema(whereNode.Name.LocalName);
            return GenerateWhere(whereRow, elementSchema);
        }

        // SQL must override
        protected virtual string GenerateWhere(XElement whereRow, XElement elementSchema)
        {
            // Oracle.
            return GenerateWhere(whereRow);
        }

        // Oracle.
        protected string GenerateWhere(XElement whereRow)
        {
            List<string> whereFragments = new List<string>();
            foreach (XElement column in whereRow.Elements())
            {
                if (column.Value.ToUpper() == "NULL")
                {
                    whereFragments.Add(string.Format("{0} IS NULL", DecorateColumnName(column.Name.LocalName)));
                }
                else
                {
                    whereFragments.Add(string.Format("{0} = {1}", DecorateColumnName(column.Name.LocalName), column.Value));
                }
            }
            string where = string.Join(" AND ", whereFragments);
            return where;
        }

        protected virtual string GenerateUpdateSql(XElement updateSetNode, XElement whereNode, XElement schema)
        {
            XElement updateSetRow = NodeToRow(updateSetNode, schema);
            if (!updateSetRow.HasElements) return string.Empty;

            IEnumerable<string> setFragments = updateSetRow.Elements().Select(p => string.Format("{0} = {1}", DecorateColumnName(p.Name.LocalName), p.Value));
            string updateSet = string.Join(", ", setFragments);

            //
            string where = GenerateDeleteOrUpdateWhere(whereNode, schema);
            string sql = string.Format("UPDATE {0} SET {1} WHERE {2}", DecorateTableName(updateSetRow.Name.LocalName), updateSet, where);
            return sql;
        }

        protected XElement NodeToRow(XElement node, XElement schema)
        {
            XElement elementSchema = schema.GetElementSchema(node.Name.LocalName);
            XElement row = new XElement(elementSchema.Attribute(Glossary.Table).Value);
            foreach (XElement field in node.Elements())
            {
                XElement fieldSchema = elementSchema.Element(field.Name);

                if (fieldSchema == null) continue;
                if (fieldSchema.Attribute(Glossary.Element) != null) continue;
                if (fieldSchema.Attribute("ReadOnly").Value == true.ToString()) continue;

                string columnName = fieldSchema.Name.LocalName;
                if (fieldSchema.Attribute(Glossary.Column) != null)
                {
                    columnName = fieldSchema.Attribute(Glossary.Column).Value;
                }
                row.SetElementValue(columnName, DecorateValue(field.Value, fieldSchema));
            }
            return row;
        }

        // remove joined & extra fileds
        protected void PurifyFileds(XElement node, XElement schema)
        {
            XElement elementSchema = schema.GetElementSchema(node.Name.LocalName);
            XElement element = new XElement(node);
            foreach (XElement field in element.Elements())
            {
                if (field.HasElements) continue;

                XElement fieldSchema = elementSchema.Element(field.Name);
                if (fieldSchema == null || fieldSchema.Attribute(Glossary.Element) != null)
                {
                    node.Element(field.Name).Remove();
                }
            }
        }

        internal protected virtual void Create(XElement element, XElement schema)
        {
            var unit = new CreateUnit(this, element, schema);
            unit.ExecuteNonQuery();
        }

        internal protected virtual void Delete(XElement element, XElement schema)
        {
            var unit = new DeleteUnit(this, element, schema);
            unit.ExecuteNonQuery();
        }

        internal protected virtual void Update(XElement element, XElement schema)
        {
            var unit = new UpdateUnit(this, element, schema, new CreateUnitFactory(), new DeleteUnitFactory());
            unit.ExecuteNonQuery();
        }

        internal protected virtual void Update(XElement element, XElement original, XElement schema)
        {
            var unit = new UpdateUnit(this, element, original, schema, new CreateUnitFactory(), new DeleteUnitFactory());
            unit.ExecuteNonQuery();
        }


    }
}
