using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.DbObjects;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public abstract partial class Database
    {
        protected class QueryView
        {
            public string TableName { get; private set; }
            public string TableAlias { get; private set; }
            public IEnumerable<Column> Columns { get; private set; }

            public IEnumerable<ForeignKeyPath> ForeignKeyPaths { get; private set; }

            public QueryView(XElement query, XElement schema)
            {
                string elementName = query.Name.LocalName;
                IEnumerable<string> fieldNames = GatherFieldNames(query);

                TableObject tableObj = TableObject.Create(fieldNames, elementName, schema);
                TableName = tableObj.TableName;
                AllocatTableAliases(tableObj);
                TableAlias = tableObj.TableAlias;
            }

            private void AllocatTableAliases(TableObject table)
            {
                var contents = table.ReferenceColumns.Select(p => p.ForeignKeyPath.Content).Distinct().OrderByDescending(p => p.Length);

                Dictionary<ReferenceColumn, string> columnDict = new Dictionary<ReferenceColumn, string>();
                Dictionary<string, ForeignKeyPath> pathDict = new Dictionary<string, ForeignKeyPath>();
                foreach (ReferenceColumn column in table.ReferenceColumns.OrderByDescending(p => p.ForeignKeyPath.Content.Length))
                {
                    string content = contents.First(p => p.StartsWith(column.ForeignKeyPath.Content));
                    columnDict.Add(column, content);
                    if (!pathDict.ContainsKey(content))
                    {
                        Debug.Assert(column.ForeignKeyPath.Content == content);

                        pathDict.Add(content, column.ForeignKeyPath);
                    }
                }

                //
                table.TableAlias = "a";

                //
                foreach (NativeColumn column in table.NativeColumns)
                {
                    column.TableAlias = "a";
                }

                //
                char letter = 'b';
                foreach (KeyValuePair<string, ForeignKeyPath> pair in pathDict)
                {
                    foreach (ForeignKey fk in pair.Value.ForeignKeys)
                    {
                        fk.TableAlias = ((char)(letter - 1)).ToString();
                        fk.RelatedTableAlias = ((char)(letter)).ToString();
                        letter++;
                    }
                    pair.Value.ForeignKeys[0].TableAlias = "a";
                }
                ForeignKeyPaths = pathDict.Values;

                //
                foreach (KeyValuePair<ReferenceColumn, string> pair in columnDict)
                {
                    pair.Key.TableAlias = pathDict[pair.Value].ForeignKeys[pair.Key.ForeignKeyPath.ForeignKeys.Length - 1].RelatedTableAlias;
                }

                List<Column> columnList = new List<Column>(table.NativeColumns);
                columnList.AddRange(table.ReferenceColumns);
                Columns = columnList;
            }

            private IEnumerable<string> GatherFieldNames(XElement query)
            {
                List<string> fieldNames = new List<string>();
                foreach (XElement field in query.Elements())
                {
                    switch (field.Name.LocalName)
                    {
                        case "Select":
                        case "OrderBy":
                        case "OrderByDescending":
                        case "ThenBy":
                        case "ThenByDescending":
                            {
                                var names = field.Elements().Select(p => p.Name.LocalName);
                                fieldNames.AddRange(names);
                            }
                            break;
                        case "Where":
                            {
                                var names = field.Descendants("MemberExpression").Select(p => p.Attribute("Member").Value);
                                fieldNames.AddRange(names);
                            }
                            break;
                    }
                }
                return fieldNames.Distinct();
            }

        }
    }
}
