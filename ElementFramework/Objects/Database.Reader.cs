using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using XData.Data.DbObjects;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public abstract partial class Database
    {
        // Element("Select")
        internal XElement GetDefault(XElement query, XElement schema)
        {
            XElement element = new XElement(query.Name);
            XElement elementSchema = schema.GetElementSchema(query.Name.LocalName);
            Dictionary<string, XElement> refElementSchemas = new Dictionary<string, XElement>();
            string sql = GenerateGetDefaultSql(query, schema);
            DataTable dataTable = CreateDataTable(sql);
            DataRow row = dataTable.Rows[0];
            foreach (DataColumn column in dataTable.Columns)
            {
                object obj = row[column];
                if (obj is System.DBNull)
                {
                    XElement nullField = new XElement(column.ColumnName, string.Empty);
                    if (schema.Attribute("Ex") != null)
                    {
                        nullField.SetAttributeValue("DataType", typeof(System.DBNull).ToString());
                    }
                    element.Add(nullField);
                    continue;
                }

                //
                Type objType;
                XElement fieldSchema = elementSchema.Element(column.ColumnName);
                XAttribute dataTypeAttr = fieldSchema.Attribute("DataType");
                if (dataTypeAttr == null)
                {
                    XAttribute elementAttr = fieldSchema.Attribute(Glossary.Element);
                    XAttribute fieldAttr = fieldSchema.Attribute(Glossary.Field);
                    if (!refElementSchemas.ContainsKey(elementAttr.Value))
                    {
                        refElementSchemas[elementAttr.Value] = schema.GetElementSchema(elementAttr.Value);
                    }
                    XElement refElementSchema = refElementSchemas[elementAttr.Value];
                    XElement refFieldSchema = refElementSchema.Element(fieldAttr.Value);
                    XAttribute refDataTypeAttr = refFieldSchema.Attribute("DataType");
                    objType = Type.GetType(refDataTypeAttr.Value);
                }
                else
                {
                    objType = Type.GetType(dataTypeAttr.Value);
                }

                //
                string value;
                if (objType == typeof(DateTime))
                {
                    if (obj is DateTime)
                    {
                        value = ((DateTime)obj).ToCanonicalString();
                    }
                    else
                    {
                        Debug.Assert(obj is string);

                        value = DateTime.Parse(obj.ToString()).ToCanonicalString();
                    }
                }
                else if (objType == typeof(byte[]))
                {
                    byte[] bytes = (byte[])obj;
                    value = "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                }
                else if (objType == typeof(bool))
                {
                    if (obj is bool)
                    {
                        value = obj.ToString();
                    }
                    else
                    {
                        if (obj.ToString() == "0")
                        {
                            value = false.ToString();
                        }
                        else
                        {
                            Debug.Assert(obj.ToString() == "1");

                            value = true.ToString();
                        }
                    }
                }
                else
                {
                    value = obj.ToString();
                }
                XElement field = new XElement(column.ColumnName, value);
                if (schema.Attribute("Ex") != null)
                {
                    field.SetAttributeValue("DataType", objType.ToString());
                }
                element.Add(field);
            }
            return element;
        }

        internal IEnumerable<T> GetSet<T>(XElement query, XElement schema, IFastGetter<T> fastGetter)
        {
            string sql = GetSetSql(query, schema);
            DataTable dataTable = CreateDataTable(sql);
            dataTable.TableName = schema.GetElementSchema(query.Name.LocalName).Attribute(Glossary.Set).Value;
            return fastGetter.ToObjects(dataTable, query.Name.LocalName, schema);
        }

        // List, Page
        internal XElement GetSet(XElement query, XElement schema)
        {
            string sql = GetSetSql(query, schema);
            IEnumerable<XElement> elements = SqlQuery(schema, query.Name.LocalName, sql);
            string setName = schema.GetElementSchema(query.Name.LocalName).Attribute(Glossary.Set).Value;
            XElement result = new XElement(setName);
            result.Add(elements);
            return result;
        }

        protected string GetSetSql(XElement query, XElement schema)
        {
            string sql;
            if (query.Element("Skip") == null)
            {
                if (query.Element("Filter") == null)
                {
                    sql = GenerateSelectSql(query, schema);
                }
                else
                {
                    XElement newQuery = new XElement(query);
                    Filter filter = CreateFilter(newQuery, schema);
                    newQuery.Element("Filter").Remove();
                    if (filter.Where != null)
                    {
                        newQuery.Add(filter.Where);
                    }
                    sql = GenerateSelectSql(newQuery, filter, schema);
                }
            }
            else
            {
                if (query.Element("Filter") == null)
                {
                    sql = GeneratePageSql(query, schema);
                }
                else
                {
                    XElement newQuery = new XElement(query);
                    Filter filter = CreateFilter(newQuery, schema);
                    newQuery.Element("Filter").Remove();
                    if (filter.Where != null)
                    {
                        newQuery.Add(filter.Where);
                    }
                    sql = GeneratePageSql(newQuery, filter, schema);
                }
            }
            return sql;
        }

        // Element("Where")
        internal int GetCount(XElement query, XElement schema)
        {
            string sql;
            if (query.Element("Filter") == null)
            {
                sql = GenerateCountSql(query, schema);
            }
            else
            {
                XElement newQuery = new XElement(query);
                Filter filter = CreateFilter(newQuery, schema);
                newQuery.Element("Filter").Remove();
                if (filter.Where != null)
                {
                    newQuery.Add(filter.Where);
                }
                sql = GenerateCountSql(newQuery, filter, schema);
            }
            object obj = ExecuteScalar(sql);
            return (int)Convert.ChangeType(obj, typeof(int));
        }

        //SELECT a.Id, a.User_Id, b.Name 
        //FROM (SELECT NULL Id, 1 User_Id) a 
        //LEFT JOIN Users b ON a.User_Id = b.Id
        protected virtual string GenerateGetDefaultSql(XElement query, XElement schema)
        {
            QueryView queryView = new QueryView(query, schema);
            string select = GenerateSelectFields(queryView.Columns);
            string leftJoins = GenerateLeftJoins(queryView.ForeignKeyPaths);

            //
            List<string> list = new List<string>();
            XElement elementSchema = schema.GetElementSchema(query.Name.LocalName);
            foreach (XElement fieldSchema in elementSchema.Elements().Where(p => p.Attribute(Glossary.Element) == null))
            {
                string column = (fieldSchema.Attribute(Glossary.Column) == null) ?
                    fieldSchema.Name.LocalName :
                    fieldSchema.Attribute(Glossary.Column).Value;
                if (fieldSchema.Element("DefaultValue") == null)
                {
                    list.Add(string.Format("NULL {0}", column));
                }
                else
                {
                    string value = fieldSchema.Element("DefaultValue").Element("Value").Value;
                    if (value == "DateTime.Now")
                    {
                        value = GetNow().ToCanonicalString();
                    }
                    else if (value == "DateTime.UtcNow")
                    {
                        value = GetUtcNow().ToCanonicalString();
                    }
                    value = DecorateValue(value, fieldSchema);
                    list.Add(string.Format("{0} {1}", value, column));
                }
            }
            string from = string.Join(",", list);

            //
            string sql = string.Format("SELECT {0} FROM (SELECT {1}) {2} {3}", select,
               from, queryView.TableAlias, leftJoins);
            return sql;
        }

        protected abstract string GeneratePageSql(XElement query, Filter filter, XElement schema);

        protected abstract string GeneratePageSql(XElement query, XElement schema);

        protected virtual string GenerateSelectSql(XElement query, Filter filter, XElement schema)
        {
            IEnumerable<string> selectFragments = query.Element("Select").Elements().Select(p => DecorateColumnName(p.Name.LocalName));
            string select = string.Join(",", selectFragments);
            string queryView = GenerateQueryViewSql(query, schema);
            string sql = string.Format("SELECT {0} FROM ({1}) t WHERE {2}", select, queryView, filter.Clause);

            //
            string orderBy = GenerateOrderByClause(query);
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                sql += " ORDER BY " + orderBy;
            }
            return sql;
        }

        //SELECT Id, Name
        //FROM
        //  (SELECT a.Id, a.User_Id, b.Name, b.FullName
        //  FROM Roles a 
        //  LEFT JOIN Users b ON a.User_Id = b.Id) t
        //WHERE User_Id = 1
        //ORDER BY FullName
        protected virtual string GenerateSelectSql(XElement query, XElement schema)
        {
            IEnumerable<string> selectFragments = query.Element("Select").Elements().Select(p => DecorateColumnName(p.Name.LocalName));
            string select = string.Join(",", selectFragments);
            string queryView = GenerateQueryViewSql(query, schema);
            string sql = string.Format("SELECT {0} FROM ({1}) t", select, queryView);
            string where = GenerateWhereClause(query, schema);
            if (!string.IsNullOrWhiteSpace(where))
            {
                sql += " WHERE " + where;
            }
            string orderBy = GenerateOrderByClause(query);
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                sql += " ORDER BY " + orderBy;
            }
            return sql;
        }

        protected virtual string GenerateCountSql(XElement query, Filter filter, XElement schema)
        {
            string queryView = GenerateQueryViewSql(query, schema);
            string sql = string.Format("SELECT COUNT(*) FROM ({0}) t WHERE {1}", queryView, filter.Clause);
            return sql;
        }

        protected virtual string GenerateCountSql(XElement query, XElement schema)
        {
            if (query.Element("Where") == null || !query.Element("Where").HasElements)
            {
                XElement elementSchema = schema.GetElementSchema(query.Name.LocalName);
                string tableName = elementSchema.Attribute(Glossary.Table).Value;
                return string.Format("SELECT COUNT(*) FROM {0}", DecorateTableName(tableName));
            }

            string queryView = GenerateQueryViewSql(query, schema);
            string sql = string.Format("SELECT COUNT(*) FROM ({0}) t", queryView);
            string where = GenerateWhereClause(query, schema);
            if (string.IsNullOrWhiteSpace(where)) return sql;
            return sql + " WHERE " + where;
        }

        protected virtual string GenerateQueryViewSql(XElement query, XElement schema)
        {
            QueryView queryView = new QueryView(query, schema);
            string select = GenerateSelectFields(queryView.Columns);
            string leftJoins = GenerateLeftJoins(queryView.ForeignKeyPaths);
            string sql = string.Format("SELECT {0} FROM {1} {2} {3}", select,
                DecorateTableName(queryView.TableName), queryView.TableAlias, leftJoins);
            return sql;
        }

        protected string GenerateSelectFields(IEnumerable<Column> columns)
        {
            IEnumerable<string> select = columns.Select(p => string.Format("{0}.{1}",
              p.TableAlias, DecorateColumnName(p.ColumnName)) +
             ((p.FieldName == p.ColumnName) ? string.Empty : " " + DecorateColumnName(p.FieldName)));
            return string.Join(",", select);
        }

        protected string GenerateLeftJoins(IEnumerable<ForeignKeyPath> foreignKeyPaths)
        {
            List<string> leftJoinList = new List<string>();
            foreach (ForeignKeyPath path in foreignKeyPaths)
            {
                foreach (ForeignKey fk in path.ForeignKeys)
                {
                    string on = string.Empty;
                    for (int i = 0; i < fk.Columns.Length; i++)
                    {
                        on += string.Format("{0}.{1} = {2}.{3}",
                            fk.TableAlias, DecorateColumnName(fk.Columns[i]),
                            fk.RelatedTableAlias, DecorateColumnName(fk.RelatedColumns[i]));
                    }
                    string leftJoin = string.Format("LEFT JOIN {0} {1} ON {2}",
                        DecorateTableName(fk.RelatedTable), fk.RelatedTableAlias, on);
                    leftJoinList.Add(leftJoin);
                }
            }
            string leftJoins = string.Join(" \r\n", leftJoinList);
            //leftJoinList.Aggregate((x, v) => string.Format(@"{0} \referencePathSchemas\n{1}", x, v));
            return leftJoins;
        }

        protected virtual string GenerateWhereClause(XElement query, XElement schema)
        {
            XElement where = query.Element("Where");
            if (where == null) return null;
            XElement elementSchema = schema.GetElementSchema(query.Name.LocalName);
            return TextureWhereClause(where.Elements().First(), elementSchema, schema);
        }

        protected string DecorateValue(string value, XElement fieldSchema, XElement schema)
        {
            XAttribute attr = fieldSchema.Attribute(Glossary.Element);
            if (attr == null)
            {
                return DecorateValue(value, fieldSchema);
            }
            else
            {
                XElement rElementSchema = schema.GetElementSchema(attr.Value);
                XElement rFieldSchema = rElementSchema.Element(fieldSchema.Attribute(Glossary.Field).Value);
                return DecorateValue(value, rFieldSchema);
            }
        }

        protected string TextureWhereClause(XElement expression, XElement elementSchema, XElement schema)
        {
            switch (expression.Name.LocalName)
            {
                case "BinaryExpression":
                    {
                        XElement leftExpression = expression.Elements().First();
                        XElement rightExpression = expression.Elements().Last();

                        string left = TextureWhereClause(leftExpression, elementSchema, schema);
                        string right = TextureWhereClause(rightExpression, elementSchema, schema);

                        string format;
                        string nodeType = expression.Attribute("NodeType").Value;
                        switch (nodeType)
                        {
                            //
                            case "AndAlso":
                                return string.Format("({0}) AND ({1}) ", left, right);
                            case "OrElse":
                                return string.Format("({0}) OR ({1}) ", left, right);
                            //
                            case "Equal":
                                if (left == "NULL")
                                {
                                    return right + " IS NULL";
                                }
                                if (right == "NULL")
                                {
                                    return left + " IS NULL";
                                }
                                format = "{0} = {1} ";
                                break;
                            case "NotEqual":
                                if (left == "NULL")
                                {
                                    return right + " IS NOT NULL";
                                }
                                if (right == "NULL")
                                {
                                    return left + " IS NOT NULL";
                                }
                                format = "{0} <> {1} ";
                                break;
                            case "GreaterThan":
                                format = "{0} > {1} ";
                                break;
                            case "GreaterThanOrEqual":
                                format = "{0} >= {1} ";
                                break;
                            case "LessThan":
                                format = "{0} < {1} ";
                                break;
                            case "LessThanOrEqual":
                                format = "{0} <= {1} ";
                                break;

                            //
                            case "Add":
                                format = "{0} + {1} ";
                                break;
                            case "Subtract":
                                format = "{0} - {1} ";
                                break;
                            case "Multiply":
                                format = "{0} * {1} ";
                                break;
                            case "Divide":
                                format = "{0} / {1} ";
                                break;
                            case "Modulo":
                                format = "{0} % {1} ";
                                break;

                            //
                            case "StartsWith":
                                format = "{0} LIKE ({1} + '%') ";
                                break;
                            case "EndsWith":
                                format = "{0} LIKE ('%' + {1}) ";
                                break;
                            case "Contains":
                                if (leftExpression.Attribute("DataType") == null)
                                {
                                    format = "{0} LIKE ('%' + {1} + '%') ";
                                    break;
                                }
                                else
                                {
                                    // IN
                                    Debug.Assert(leftExpression.Attribute("DataType").Value == "System.Collections.IList");

                                    string fieldName = rightExpression.Attribute("Member").Value;
                                    XElement fieldSchema = elementSchema.Element(fieldName);
                                    string[] fragments = leftExpression.Attribute("Value").Value.Split(',');
                                    for (int i = 0; i < fragments.Length; i++)
                                    {
                                        fragments[i] = DecorateValue(fragments[i], fieldSchema, schema);
                                    }
                                    string leftValue = string.Join(",", fragments);
                                    return string.Format("{0} IN ({1})", right, leftValue);
                                }
                            default:
                                throw new NotSupportedException(nodeType);
                        }
                        if (leftExpression.Name.LocalName == "ConstantExpression")
                        {
                            Type dataType = Type.GetType(leftExpression.Attribute("DataType").Value);
                            //if (dataType == typeof(String) && rightExpression.Name.LocalName == "MemberExpression")
                            if (rightExpression.Name.LocalName == "MemberExpression")
                            {
                                string fieldName = rightExpression.Attribute("Member").Value;
                                XElement fieldSchema = elementSchema.Element(fieldName);

                                // DecorateValue instead of DecorateStringValue
                                // so that support short cut
                                left = DecorateValue(left, fieldSchema, schema);
                            }
                            else
                            {
                                left = DecorateValue(left, dataType);
                            }
                        }
                        if (rightExpression.Name.LocalName == "ConstantExpression")
                        {
                            Type dataType = Type.GetType(rightExpression.Attribute("DataType").Value);
                            //if (dataType == typeof(String) && leftExpression.Name.LocalName == "MemberExpression")
                            if (leftExpression.Name.LocalName == "MemberExpression")
                            {
                                string fieldName = leftExpression.Attribute("Member").Value;
                                XElement fieldSchema = elementSchema.Element(fieldName);

                                // DecorateValue instead of DecorateStringValue
                                // so that support short cut
                                right = DecorateValue(right, fieldSchema, schema);
                            }
                            else
                            {
                                right = DecorateValue(right, dataType);
                            }
                        }
                        if (leftExpression.Name.LocalName == "MemberExpression")
                        {
                            left = DecorateColumnName(left);
                        }
                        if (rightExpression.Name.LocalName == "MemberExpression")
                        {
                            right = DecorateColumnName(right);
                        }
                        return string.Format(format, left, right);
                    }
                case "UnaryExpression":
                    {
                        string nodeType = expression.Attribute("NodeType").Value;
                        switch (nodeType)
                        {
                            case "Negate":
                                return string.Format("-{0} ", TextureWhereClause(expression.Elements().First(), elementSchema, schema));
                            case "Not":
                                return string.Format("NOT ({0}) ", TextureWhereClause(expression.Elements().First(), elementSchema, schema));
                        }
                    }
                    break;
                case "MemberExpression":
                    {
                        string fieldName = expression.Attribute("Member").Value;
                        return fieldName;
                    }
                case "ConstantExpression":
                    {
                        XAttribute attr = expression.Attribute("Value");
                        if (attr == null) return "NULL";
                        return attr.Value;
                    }
            }

            throw new NotSupportedException(expression.ToString());
        }

        protected string GenerateOrderByClause(XElement query)
        {
            List<string> list = new List<string>();
            foreach (XElement element in query.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "OrderBy":
                    case "ThenBy":
                        list.Add(DecorateColumnName(element.Elements().First().Name.LocalName) + " ASC");
                        break;
                    case "OrderByDescending":
                    case "ThenByDescending":
                        list.Add(DecorateColumnName(element.Elements().First().Name.LocalName) + " DESC");
                        break;
                }
            }
            return string.Join(",", list);
        }

        // RelatedObject
        internal XElement GetRelatedSet(XElement element, RelationshipPath reversedRelationshipPath,
            int elementAsChildRelatedAliasIndex, string elementAsChildRelatedElement, Dictionary<string, string> elementAsChildRelatedFieldAliases,
            XElement query, XElement schema, string relatedElementAlias, string relatedSetName)
        {
            string sql = GenerateGetRelatedSetSql(element, reversedRelationshipPath,
                elementAsChildRelatedAliasIndex, elementAsChildRelatedElement, elementAsChildRelatedFieldAliases,
                query, schema);
            IEnumerable<XElement> elements;
            if (string.IsNullOrWhiteSpace(relatedElementAlias) || relatedElementAlias == query.Name.LocalName)
            {
                elements = SqlQuery(schema, query.Name.LocalName, sql);
                string setName = schema.GetElementSchema(query.Name.LocalName).Attribute(Glossary.Set).Value;
                if (string.IsNullOrWhiteSpace(relatedSetName) || relatedSetName == setName)
                {
                    XElement resultSet = new XElement(setName);
                    resultSet.Add(elements);
                    return resultSet;
                }
            }
            else
            {
                elements = SqlQuery(schema, relatedElementAlias, sql);
            }
            if (string.IsNullOrWhiteSpace(relatedSetName)) throw new ArgumentNullException("relatedSetName");
            XElement result = new XElement(relatedSetName);
            result.Add(elements);
            return result;
        }

        //SELECT * FROM 
        //  (SELECT a.*, b.Name 
        //   FROM 
        //      (SELECT t1.*, t3.RoleId Column_{Guid} FROM Users t1 INNER JOIN UserRoles t2 ON a1.UserId = t2.UserId INNER JOIN Roles t3 ON t3.RoleId = t2.RoleId
        //       AND t3.RoleId = 1) a
        //   LEFT JOIN People b ON a.PersonId = b.PersonId) t
        //ORDER BY
        protected virtual string GenerateGetRelatedSetSql(XElement element, RelationshipPath reversedRelationshipPath,
            int elementAsChildRelatedAliasIndex, string elementAsChildRelatedElement, Dictionary<string, string> elementAsChildRelatedFieldAliases,
            XElement query, XElement schema)
        {
            XElement newQuery = new XElement(query);

            //
            Filter filter = null;
            if (newQuery.Element("Filter") != null)
            {
                filter = CreateFilter(newQuery, schema);
                newQuery.Element("Filter").Remove();
                if (filter.Where != null)
                {
                    newQuery.Add(filter.Where);
                }
            }

            //
            IEnumerable<string> selectFragments = newQuery.Element("Select").Elements().Select(p => DecorateColumnName(p.Name.LocalName));
            string select = string.Join(",", selectFragments);
            string eacrColumnAliases = string.Join(",", elementAsChildRelatedFieldAliases.Values);

            string queryView = GenerateQueryViewSqlForGetRelatedSet(element, reversedRelationshipPath,
                elementAsChildRelatedAliasIndex, elementAsChildRelatedElement, elementAsChildRelatedFieldAliases,
                newQuery, schema);
            string sql = string.Format("SELECT {0},{1} FROM ({2}) t", select, eacrColumnAliases, queryView);

            //
            string where;
            if (filter == null)
            {
                where = GenerateWhereClause(newQuery, schema);
            }
            else
            {
                where = filter.Clause;
            }
            if (!string.IsNullOrWhiteSpace(where))
            {
                sql += " WHERE " + where;
            }

            string orderBy = GenerateOrderByClause(newQuery);
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                sql += " ORDER BY " + orderBy;
            }
            return sql;
        }

        protected virtual string GenerateQueryViewSqlForGetRelatedSet(XElement element, RelationshipPath reversedRelationshipPath,
             int elementAsChildRelatedAliasIndex, string elementAsChildRelatedElement, Dictionary<string, string> elementAsChildRelatedFieldAliases,
            XElement query, XElement schema)
        {
            string innerJoin = GenerateInnerJoinsForGetRelatedSet(reversedRelationshipPath.Relationships, schema);

            //
            string afterInnerJoinsAnd = string.Empty;
            XElement elementSchema = schema.GetElementSchema(element.Name.LocalName);
            string elementTableAlias = "t" + (reversedRelationshipPath.Relationships.Count() + 1).ToString();
            foreach (string fieldName in reversedRelationshipPath.Relationships.Last().RelatedFieldNames)
            {
                XElement fieldSchema = elementSchema.Element(fieldName);
                string columnName = (fieldSchema.Attribute(Glossary.Column) == null) ?
                    fieldSchema.Name.LocalName : fieldSchema.Attribute(Glossary.Column).Value;
                string value = element.Element(fieldName).Value;
                afterInnerJoinsAnd += string.Format("AND {0}.{1} = {2}", elementTableAlias, DecorateColumnName(columnName), DecorateValue(value, fieldSchema));
            }

            //
            XElement earcElementSchema = schema.GetElementSchema(elementAsChildRelatedElement);
            string earcTableAlias = "t" + (elementAsChildRelatedAliasIndex + 1).ToString();
            List<string> earcColumnList = new List<string>();
            foreach (var pair in elementAsChildRelatedFieldAliases)
            {
                string fieldName = pair.Key;
                XElement fieldSchema = earcElementSchema.Element(fieldName);
                string columnName = (fieldSchema.Attribute(Glossary.Column) == null) ? fieldName : fieldSchema.Attribute(Glossary.Column).Value;
                string earcColumnStr = string.Format("{0}.{1} {2}", earcTableAlias, DecorateColumnName(columnName), pair.Value);
                earcColumnList.Add(earcColumnStr);
            }
            string eacrColumns = string.Join(",", earcColumnList);

            QueryView queryView = new QueryView(query, schema);
            string innerSql = string.Format("SELECT t1.*,{0} FROM {1} t1 {2} {3}", eacrColumns, DecorateTableName(queryView.TableName), innerJoin, afterInnerJoinsAnd);

            string select = GenerateSelectFields(queryView.Columns);
            string eacrColumnAliases = string.Join(",", elementAsChildRelatedFieldAliases.Values);
            string leftJoins = GenerateLeftJoins(queryView.ForeignKeyPaths);
            string sql = string.Format("SELECT {0},{1} FROM ({2}) {3} {4}", select, eacrColumnAliases, innerSql, queryView.TableAlias, leftJoins);
            return sql;
        }

        protected string GenerateInnerJoinsForGetRelatedSet(SimpleRelationship[] relationships, XElement schema)
        {
            List<string> joinList = new List<string>();
            int aliasIndex = 2;
            foreach (SimpleRelationship relationship in relationships)
            {
                ManyToOneRelationship manyToOneRelationship = new Schema.ManyToOneRelationship(relationship.Content);
                ForeignKey fk = new ForeignKey(manyToOneRelationship, schema);
                fk.TableAlias = "t" + (aliasIndex - 1).ToString();
                fk.RelatedTableAlias = "t" + aliasIndex.ToString();
                string on = string.Empty;
                for (int i = 0; i < fk.Columns.Length; i++)
                {
                    on += string.Format("{0}.{1} = {2}.{3}",
                        fk.TableAlias, DecorateColumnName(fk.Columns[i]),
                        fk.RelatedTableAlias, DecorateColumnName(fk.RelatedColumns[i]));
                }
                string join = string.Format("INNER JOIN {0} {1} ON {2}",
                    DecorateTableName(fk.RelatedTable), fk.RelatedTableAlias, on);
                joinList.Add(join);
                aliasIndex++;
            }
            string joins = string.Join(" \r\n", joinList);
            return joins;
        }

        protected abstract Filter CreateFilter(XElement query, XElement schema);


    }
}
