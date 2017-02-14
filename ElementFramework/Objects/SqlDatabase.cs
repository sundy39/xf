using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Objects
{
    // SQL Server 2012
    // not supported:
    // datetime2(7);datetimeoffset(7);geography;geometry
    // hierarchyid;image;money;ntext;smallmoney;sql_variant;
    // text;time(7);xml
    public partial class SqlDatabase : Database
    {
        // DataSet  ExtendedProperties["DataSetVersion"] = GetUtcNow().Ticks.DecorateValue();  
        //          ExtendedProperties["TimezoneOffset"] = "+12:00"
        // elementObj    ExtendedProperties["TableType"] = "elementObj" or "View";
        //          ExtendedProperties["RowVersion"] = column.ColumnName;
        // Column   ExtendedProperties["DefaultValue"] = "DateTime.Now" or "DateTime.UtcNow" or "Guid.NewGuid()" or "0x12AD"; 
        //          ExtendedProperties["DataType"] = nvachar/int/datetime;
        protected override DataSet CreateSchemaDataSet()
        {
            DataSet schemaDataSet = new DataSet("Schema");

            //
            Dictionary<string, string> fkDictionary = new Dictionary<string, string>();
            DataTable fkTable = new DataTable();
            DateTime utcNow;
            int timezoneMinutes;
            DataTable sequencesTable = new DataTable("Sequences");
            //
            SqlCommand cmd = base.Connection.CreateCommand() as SqlCommand;
            cmd.CommandText = @"SELECT TABLE_NAME, TABLE_TYPE FROM INFORMATION_SCHEMA.TABLES;";
            ConnectionState state = cmd.Connection.State;
            try
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    DataTable dt = new DataTable(dr.GetString(0));
                    string tableType = dr.GetString(1);
                    if (tableType == "BASE TABLE")
                    {
                        tableType = "Table";
                    }
                    else if (tableType == "VIEW")
                    {
                        tableType = "View";
                    }
                    dt.ExtendedProperties["TableType"] = tableType;
                    schemaDataSet.Tables.Add(dt);
                }
                dr.Close();

                // FillSchema
                foreach (DataTable dt in schemaDataSet.Tables)
                {
                    cmd.CommandText = string.Format("SELECT * FROM \"{0}\";", dt.TableName);
                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = cmd;
                    try
                    {
                        da.FillSchema(dt, SchemaType.Source);
                    }
                    catch
                    {
                        // dt is wrong View
                        if (dt.ExtendedProperties["TableType"].ToString() != "View") throw;
                    }
                }

                // default_value, data_type, rowversion/timestamp
                cmd.CommandText = @"SELECT TABLE_NAME, COLUMN_NAME, COLUMN_DEFAULT, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS;";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    DataTable dt = schemaDataSet.Tables[dr.GetString(0)];
                    string columnName = dr.GetString(1);
                    DataColumn col = dt.Columns[columnName];

                    // dt is wrong View
                    if (col == null) continue;

                    if (dr.GetValue(2) != DBNull.Value)
                    {
                        string defVal = dr.GetString(2);
                        while (defVal.StartsWith("(") && defVal.EndsWith(")"))
                        {
                            defVal = defVal.Substring(1, defVal.Length - 2);
                        }

                        //
                        if (col.DataType == typeof(DateTime))
                        {
                            if (defVal.ToLower().Contains("getdate()"))
                            {
                                col.ExtendedProperties["DefaultValue"] = "DateTime.Now";
                            }
                            else if (defVal.ToLower().Contains("getutcdate()"))
                            {
                                col.ExtendedProperties["DefaultValue"] = "DateTime.UtcNow";
                            }
                            else
                            {
                                defVal = defVal.TrimStart('\'').TrimEnd('\'');
                                col.DefaultValue = Convert.ToDateTime(defVal);
                            }
                        }
                        else if (col.DataType == typeof(Guid))
                        {
                            if (defVal.ToLower().Contains("newid()"))
                            {
                                col.ExtendedProperties["DefaultValue"] = "Guid.NewGuid()";
                            }
                            else
                            {
                                defVal = defVal.TrimStart('\'').TrimEnd('\'');
                                col.DefaultValue = Guid.Parse(defVal);
                            }
                        }
                        else if (col.DataType == typeof(byte[])) // varbinary(n)/rowversion/timestamp
                        {
                            defVal = defVal.TrimStart('\'').TrimEnd('\'');
                            col.ExtendedProperties["DefaultValue"] = defVal;
                        }
                        else if (col.DataType == typeof(bool))
                        {
                            if (defVal == "0")
                            {
                                col.DefaultValue = false;
                            }
                            else if (defVal == "1")
                            {
                                col.DefaultValue = true;
                            }
                        }
                        else if (col.DataType == typeof(string))
                        {
                            int index = defVal.IndexOf('\'');
                            if (index == -1)
                            {
                                col.DefaultValue = defVal;
                            }
                            else
                            {
                                col.DefaultValue = defVal.Substring(index).TrimStart('\'').TrimEnd('\'');
                            }
                        }
                        else
                        {
                            col.DefaultValue = Convert.ChangeType(defVal, col.DataType);
                        }
                    }
                    //
                    col.ExtendedProperties["DataType"] = dr.GetValue(3);
                }
                dr.Close();

                // fkDictionary
                cmd.CommandText = @"SELECT CONSTRAINT_NAME, UNIQUE_CONSTRAINT_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS;";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    fkDictionary.Add(dr.GetString(0), dr.GetString(1));
                }
                dr.Close();

                // fkTable                 
                cmd.CommandText = @"SELECT TABLE_NAME, COLUMN_NAME, CONSTRAINT_NAME FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE;";
                new SqlDataAdapter(cmd).Fill(fkTable);

                // utcNow
                cmd.CommandText = "SELECT GETUTCDATE()";
                utcNow = (DateTime)cmd.ExecuteScalar();

                //
                cmd.CommandText = "SELECT DATEDIFF(MINUTE,GETUTCDATE(),GETDATE())";
                timezoneMinutes = (int)cmd.ExecuteScalar();

                // sequencesTable
                cmd.CommandText = "SELECT SEQUENCE_NAME FROM INFORMATION_SCHEMA.SEQUENCES;";
                new SqlDataAdapter(cmd).Fill(sequencesTable);
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }

            // FK
            foreach (KeyValuePair<string, string> pair in fkDictionary)
            {
                string fkName = pair.Key;
                string rkName = pair.Value;
                DataRow[] fkRows = fkTable.Select(string.Format("CONSTRAINT_NAME = '{0}'", fkName));
                DataRow[] rkRows = fkTable.Select(string.Format("CONSTRAINT_NAME = '{0}'", rkName));
                DataColumn[] fkColumns = new DataColumn[fkRows.Length];
                string childTableName = fkRows[0]["TABLE_NAME"].ToString();
                for (int i = 0; i < fkRows.Length; i++)
                {
                    DataRow row = fkRows[i];
                    fkColumns[i] = schemaDataSet.Tables[row["TABLE_NAME"].ToString()].Columns[row["COLUMN_NAME"].ToString()];
                }
                DataColumn[] rkColumns = new DataColumn[rkRows.Length];
                for (int i = 0; i < rkRows.Length; i++)
                {
                    DataRow row = rkRows[i];
                    rkColumns[i] = schemaDataSet.Tables[row["TABLE_NAME"].ToString()].Columns[row["COLUMN_NAME"].ToString()];
                }
                ForeignKeyConstraint fk = new ForeignKeyConstraint(fkName, rkColumns, fkColumns);
                schemaDataSet.Tables[childTableName].Constraints.Add(fk);
            }

            //         
            schemaDataSet.ExtendedProperties["TimezoneOffset"] = new TimezoneOffset(timezoneMinutes).ToString();

            //
            schemaDataSet.ExtendedProperties["DataSetVersion"] = utcNow.Ticks.ToString();

            if (schemaDataSet.Tables.Contains("sysdiagrams"))
            {
                schemaDataSet.Tables.Remove("sysdiagrams");
            }

            schemaDataSet.ExtendedProperties["DatabaseSequences"] = sequencesTable;
            return schemaDataSet;
        }

        internal protected override DateTime GetUtcNow()
        {
            return (DateTime)ExecuteScalar("SELECT GETUTCDATE()");
        }

        internal protected override DateTime GetNow()
        {
            return (DateTime)ExecuteScalar("SELECT GETDATE()");
        }

        protected override DbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        protected override DbDataAdapter CreateDbDataAdapter()
        {
            return new SqlDataAdapter();
        }

        public override DbParameter CreateDbParameter(string parameterName, object obj)
        {
            return new SqlParameter(parameterName, obj);
        }

        public SqlDatabase(string connectionString, string databaseVersion)
            : base(connectionString, databaseVersion)
        {
        }

        internal protected override object GenerateSequence(string sequenceName)
        {
            string sql = string.Format("SELECT NEXT VALUE FOR [{0}]", sequenceName);
            SqlConnection conn = new SqlConnection(Connection.ConnectionString);
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            try
            {
                conn.Open();
                return cmd.ExecuteScalar();
            }
            finally
            {
                conn.Close();
            }
        }

        protected override string DecorateTableName(string tableName)
        {
            return string.Format("[{0}]", tableName);
        }

        protected override string DecorateColumnName(string columnName)
        {
            return string.Format("[{0}]", columnName);
        }

        protected override string DecorateStringValue(string value, XElement fieldSchema)
        {
            if (string.IsNullOrEmpty(value)) return DecorateEmptyStringValue(fieldSchema);

            string result = "'" + value.Replace("'", "''") + "'";
            if (fieldSchema.Attribute("SqlDbType").Value.StartsWith("n"))
            {
                result = "N" + result;
            }
            return result;
        }

        protected override string DecorateDateTimeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";
            return "'" + DateTime.Parse(value).ToString("yyyy-MM-dd HH:mm:ss.FFF") + "'";
        }

        protected override string GeneratePageSql(XElement query, Filter filter, XElement schema)
        {
            string selectSql = GenerateSelectSql(query, filter, schema);
            return GeneratePageSql(selectSql, query);
        }

        protected override string GeneratePageSql(XElement query, XElement schema)
        {
            string selectSql = GenerateSelectSql(query, schema);
            return GeneratePageSql(selectSql, query);
        }

        protected string GeneratePageSql(string selectSql, XElement query)
        {
            string sql = selectSql + string.Format(" OFFSET {0} ROWS", query.Element("Skip").Value);
            if (query.Element("Take") != null)
            {
                sql += string.Format(" FETCH NEXT {0} ROWS ONLY", query.Element("Take").Value);
            }
            return sql;
        }

        protected override string GenerateWhere(XElement whereRow, XElement elementSchema)
        {
            List<string> whereFragments = new List<string>();
            foreach (XElement column in whereRow.Elements())
            {
                XElement fieldSchema = elementSchema.Elements().FirstOrDefault(p => p.Attribute(Glossary.Column) != null && p.Attribute(Glossary.Column).Value == column.Name.LocalName);
                if (fieldSchema == null) fieldSchema = elementSchema.Element(column.Name);
                if (Type.GetType(fieldSchema.Attribute("DataType").Value) == typeof(string))
                {
                    if (column.Value == DecorateStringValue(string.Empty, fieldSchema))
                    {
                        if (fieldSchema.Attribute("AllowDBNull").Value == true.ToString())
                        {
                            whereFragments.Add(string.Format("({0} IS NULL OR {0} = {1})", DecorateColumnName(column.Name.LocalName), column.Value));
                            continue;
                        }
                    }
                }

                //
                if (column.Value.ToUpper() == "NULL")
                {
                    whereFragments.Add(string.Format("({0} IS NULL)", DecorateColumnName(column.Name.LocalName)));
                }
                else
                {
                    whereFragments.Add(string.Format("{0} = {1}", DecorateColumnName(column.Name.LocalName), column.Value));
                }
            }
            string where = string.Join(" AND ", whereFragments);
            return where;
        }

        protected override Filter CreateFilter(XElement query, XElement schema)
        {
            return new SqlFilter(query, schema, this);
        }

        protected override string GenerateInsertSql(XElement node, XElement schema, out DbParameter[] parameters)
        {
            return base.GenerateInsertSql(node, schema, out parameters);
        }


    }
}
