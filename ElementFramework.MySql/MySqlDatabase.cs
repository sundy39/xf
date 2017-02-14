using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    // MySQL 5.5.47
    //INT
    //VARCHAR()
    //DECIMAL()
    //DATETIME()
    //BLOB

    //BINARY()
    //BLOB()
    //LONGBLOB
    //MEDIUMBLOB
    //TINYBLOB
    //VARBINARY()

    //DATE      System.TimeSpan
    //DATETIME
    //TIME      System.DateTime
    //TIMESTAMP
    //YEAR()    System.Int32

    //GEOMETRY              N
    //GEOMETRYCOLLECTION    N
    //LINESTRING            N
    //MUTILINESTRING        N
    //MUTIPOINT             N
    //MOTIPOLYGON           N
    //POINT                 N
    //POLYGON               N

    //BIGINT()
    //DECIMAL
    //DOUBLE
    //FLOAT
    //INT()
    //MEDIUMINT()
    //REAL
    //SMALLINT()
    //TINYINT()

    //CHAR()
    //NVARCHAR()
    //VARCHAR()

    //LONGTEXT
    //MEDIUNTEXT
    //TEXT()
    //TINYTEXT

    //BIT()
    //ENUM()
    //SET()
    public partial class MySqlDatabase : Database
    {
        protected readonly string DatabaseName;

        public MySqlDatabase(string connectionString, string databaseVersion)
            : base(connectionString, databaseVersion)
        {
            string[] ss = connectionString.Split(';');
            string databaseName = ss.First(s => s.Trim().StartsWith("database"));
            int index = databaseName.IndexOf('=');
            databaseName = databaseName.Substring(index + 1);
            DatabaseName = databaseName.Trim();
        }

        protected override DbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        protected override DbDataAdapter CreateDbDataAdapter()
        {
            return new MySqlDataAdapter();
        }

        protected override DataSet CreateSchemaDataSet()
        {
            DataSet schemaDataSet = new DataSet("Schema");

            //
            DataTable fkTable = new DataTable();
            DateTime utcNow;

            //
            MySqlCommand cmd = base.Connection.CreateCommand() as MySqlCommand;
            cmd.CommandText = string.Format(@"SELECT table_name, table_type FROM information_schema.tables WHERE table_schema = '{0}'", DatabaseName);
            ConnectionState state = cmd.Connection.State;
            try
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }
                MySqlDataReader dr = cmd.ExecuteReader();
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
                    cmd.CommandText = string.Format("SELECT * FROM {0}", dt.TableName);
                    MySqlDataAdapter da = new MySqlDataAdapter();
                    da.SelectCommand = cmd;
                    da.FillSchema(dt, SchemaType.Source);
                }

                // default_value, data_type
                cmd.CommandText = string.Format(@"
SELECT table_name, column_name, column_default, is_nullable, data_type, extra FROM information_schema.COLUMNS WHERE table_schema = '{0}'",
DatabaseName);
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    DataTable dt = schemaDataSet.Tables[dr.GetString(0)];
                    string columnName = dr.GetString(1);
                    DataColumn col = dt.Columns[columnName];
                    col.AllowDBNull = dr.GetString(3) == "YES";
                    col.ExtendedProperties["DataType"] = dr.GetValue(4);
                    if (dr.GetValue(2) != DBNull.Value)
                    {
                        string defVal = dr.GetString(2);
                        if (col.DataType == typeof(DateTime))
                        {
                            if (defVal == "CURRENT_TIMESTAMP")
                            {
                                col.ExtendedProperties["DefaultValue"] = "DateTime.Now";
                            }
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
                    if (dr.GetValue(5) != DBNull.Value)
                    {
                        if (dr.GetString(5) == "auto_increment")
                        {
                            col.AutoIncrement = true;
                        }
                    }
                }
                dr.Close();

                // fkTable                               
                cmd.CommandText = string.Format(@"
SELECT c.constraint_name, c.table_name, c.column_name, c.referenced_table_name, c.referenced_column_name
FROM information_schema.key_column_usage c 
INNER JOIN information_schema.table_constraints t
ON c.constraint_schema = t.constraint_schema
AND c.constraint_name = t.constraint_name
AND t.constraint_type='FOREIGN KEY'
AND t.constraint_schema='{0}'", DatabaseName);
                new MySqlDataAdapter(cmd).Fill(fkTable);

                // utcNow
                cmd.CommandText = "SELECT utc_timestamp()";
                utcNow = (DateTime)cmd.ExecuteScalar();
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }

            // FK
            Dictionary<string, List<DataRow>> dictionary = new Dictionary<string, List<DataRow>>();
            foreach (DataRow row in fkTable.Rows)
            {
                string key = row["constraint_name"].ToString();
                if (!dictionary.ContainsKey(key))
                {
                    dictionary[key] = new List<DataRow>();
                }
                dictionary[key].Add(row);
            }

            List<DbForeignKey> foreignKeys = new List<DbForeignKey>();
            foreach (KeyValuePair<string, List<DataRow>> pair in dictionary)
            {
                string constraintName = pair.Key;
                List<DataRow> rows = pair.Value;
                string tableName = rows[0]["table_name"].ToString();
                string r_tableName = rows[0]["referenced_table_name"].ToString();

                if (schemaDataSet.Tables[tableName] == null) continue;
                if (schemaDataSet.Tables[r_tableName] == null) continue;

                // distinct
                Dictionary<string, object> columns = new Dictionary<string, object>();
                Dictionary<string, object> r_columns = new Dictionary<string, object>();
                foreach (DataRow row in rows)
                {
                    columns[row["column_name"].ToString()] = null;
                    r_columns[row["referenced_column_name"].ToString()] = null;
                }
                string[] columnNames = columns.Keys.ToArray();
                string[] r_columnNames = r_columns.Keys.ToArray();

                var fk = new DbForeignKey(constraintName, tableName, columnNames, r_tableName, r_columnNames);
                foreignKeys.Add(fk);
            }
            schemaDataSet.ExtendedProperties["ForeignKeys"] = foreignKeys;

            //
            schemaDataSet.ExtendedProperties["DataSetVersion"] = utcNow.Ticks.ToString();

            return schemaDataSet;
        }

        protected override DateTime GetUtcNow()
        {
            return (DateTime)ExecuteScalar("SELECT utc_timestamp()");
        }

        protected override DateTime GetNow()
        {
            return (DateTime)ExecuteScalar("SELECT current_timestamp()");
        }

        protected override string GeneratePageSql(XElement query, Database.Filter filter, XElement schema)
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
            XElement xSkip = query.Element("Skip");
            string skip = (xSkip == null) ? "0" : xSkip.Value;
            string take = query.Element("Take").Value;

            string sql = selectSql + string.Format(" LIMIT {0},{1}", skip, take);
            return sql;
        }

        protected override Database.Filter CreateFilter(XElement query, XElement schema)
        {
            return new MySqlFilter(query, schema, this);
        }

        protected override object GenerateSequence(string sequenceName)
        {
            //string sql = string.Format("SELECT nextval({0})", sequenceName);
            //MySqlConnection conn = new MySqlConnection(Connection.ConnectionString);
            //DbCommand cmd = conn.CreateCommand();
            //cmd.CommandText = sql;
            //try
            //{
            //    conn.Open();
            //    return cmd.ExecuteScalar();
            //}
            //finally
            //{
            //    conn.Close();
            //}
            throw new NotImplementedException();
        }

        protected override string DecorateTableName(string tableName)
        {
            return "`" + tableName + "`";
        }

        protected override string DecorateColumnName(string columnName)
        {
            return "`" + columnName + "`";
        }

        protected override string DecorateStringValue(string value, XElement fieldSchema)
        {
            if (string.IsNullOrEmpty(value) &&
                fieldSchema.Attribute("AllowDBNull").Value == true.ToString() &&
                fieldSchema.Element("Required") == null)
            {
                return "NULL";
            }

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

            string result = DateTime.Parse(value).ToString("yyyy-MM-dd HH:mm:ss");
            return "'" + result + "'";
        }

        protected override string DecorateTimeSpanValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";

            string result = TimeSpan.Parse(value).ToString("hh:mm:ss");
            return "'" + result + "'";
        }

        protected override string DecorateNumberValue(string value, XElement fieldSchema)
        {
            if (fieldSchema.Attribute("DataType=").Value == "System.UInt64" &&
                fieldSchema.Attribute("SqlDbType=").Value == "bit")
            {
                UInt64 uiValue = UInt64.Parse(value);
                string result;
                if (uiValue > Int64.MaxValue)
                {
                    byte[] bytes = BitConverter.GetBytes(uiValue);
                    result = string.Empty;
                    for (int i = bytes.Length - 1; i >= 0; i--)
                    {
                        string s = Convert.ToString(bytes[i], 2);
                        result += new string('0', 8 - s.Length) + s;
                    }
                }
                else
                {
                    result = Convert.ToString((Int64)uiValue, 2);
                }
                return "b'" + result + "'";
            }
            return base.DecorateNumberValue(value, fieldSchema);
        }

        protected override string DecorateByteArrayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";

            Debug.Assert(value.StartsWith("0x"));

            string result = value.Substring(2);
            return string.Format("UNHEX('{0}')", result);
        }


    }
}
