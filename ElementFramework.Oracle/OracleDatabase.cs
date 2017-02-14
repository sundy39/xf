using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects.Oracle;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    // 11g
    // not supported:
    // LONG;RAW;LONG RAW;BFILE;
    // INTERVAL YEAR TO MONTH;INTERVAL DAY TO SECOND;
    // TIMESTAMP WITH TIME ZONE;TIMESTAMP WITH LOCAL TIME ZONE
    public partial class OracleDatabase : Database
    {
        public OracleDatabase(string connectionString, string databaseVersion)
            : base(connectionString, databaseVersion)
        {
        }

        protected override DbConnection CreateConnection(string connectionString)
        {
            return new OracleConnection(connectionString);
        }

        protected override DbDataAdapter CreateDbDataAdapter()
        {
            return new OracleDataAdapter();
        }

        public override DbParameter CreateDbParameter(string parameterName, object obj)
        {
            return new OracleParameter(parameterName, obj);
        }

        protected override DataSet CreateSchemaDataSet()
        {
            DataSet schemaDataSet = new DataSet("Schema");

            DataTable fkTable = new DataTable();
            DateTime now;
            string timeZone;
            DataTable sequencesTable = new DataTable("Sequences");

            OracleCommand cmd = base.Connection.CreateCommand() as OracleCommand;
            cmd.CommandText = @"SELECT TABLE_NAME FROM USER_TABLES";
            ConnectionState state = cmd.Connection.State;
            try
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }
                OracleDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    string tableName = dr.GetString(0);
                    if (tableName.Contains('$')) continue;
                    DataTable dt = new DataTable(tableName);
                    dt.ExtendedProperties["TableType"] = "Table";
                    schemaDataSet.Tables.Add(dt);
                }
                dr.Close();

                //    
                cmd.CommandText = @"SELECT VIEW_NAME FROM USER_VIEWS";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    string tableName = dr.GetString(0);
                    if (tableName.Contains('$')) continue;
                    DataTable dt = new DataTable(tableName);
                    dt.ExtendedProperties["TableType"] = "View";
                    schemaDataSet.Tables.Add(dt);
                }
                dr.Close();

                // FillSchema
                foreach (DataTable dt in schemaDataSet.Tables)
                {
                    cmd.CommandText = string.Format("SELECT * FROM \"{0}\"", dt.TableName);
                    OracleDataAdapter da = new OracleDataAdapter();
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

                // DATA_DEFAULT is LONG
                string tempTableName = GetTemporaryTableName(cmd);
                cmd.CommandText = string.Format(
                    "CREATE GLOBAL TEMPORARY TABLE {0} ON COMMIT PRESERVE ROWS AS SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, NULLABLE, TO_LOB(DATA_DEFAULT) DATA_DEFAULT FROM USER_TAB_COLUMNS",
                    tempTableName);
                int i = cmd.ExecuteNonQuery();
                try
                {
                    cmd.CommandText = string.Format("SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, NULLABLE, DATA_DEFAULT FROM {0}", tempTableName);
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        DataTable dt = schemaDataSet.Tables[dr.GetString(0)];
                        if (dt == null) continue;
                        string columnName = dr.GetString(1);
                        DataColumn col = dt.Columns[columnName];

                        // dt is wrong View
                        if (col == null) continue;

                        col.ExtendedProperties["DataType"] = dr.GetString(2);
                        col.AllowDBNull = dr.GetString(3) == "Y";
                        if (dr.GetValue(4) != DBNull.Value)
                        {
                            string defVal = dr.GetString(4);

                            if (col.DataType == typeof(DateTime))
                            {
                                defVal = defVal.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
                                if (defVal == string.Empty || defVal.ToLower() == "null") continue;

                                if (defVal == "SYSDATE")
                                {
                                    col.ExtendedProperties["DefaultValue"] = "DateTime.Now";
                                }
                                else if (defVal == "CURRENT_TIMESTAMP")
                                {
                                    col.ExtendedProperties["DefaultValue"] = "DateTime.Now";
                                }
                            }
                            else if (col.DataType == typeof(byte[]))
                            {
                                defVal = defVal.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
                                if (defVal == string.Empty || defVal.ToLower() == "null") continue;

                                defVal = defVal.TrimStart('\'').TrimEnd('\'');
                                col.ExtendedProperties["DefaultValue"] = defVal;
                            }
                            else if (col.DataType == typeof(string))
                            {
                                if (defVal.Trim() == "NULL") continue;

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
                            else if (IsNumberType(col.DataType))
                            {
                                defVal = defVal.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
                                if (defVal == string.Empty || defVal.ToLower() == "null") continue;

                                defVal = defVal.TrimStart(new char[] { '(', '\'', '"' }).TrimEnd(new char[] { ')', '\'', '"' });
                                if (defVal != string.Empty && defVal.ToLower() != "null")
                                {
                                    double result;
                                    if (double.TryParse(defVal, out result))
                                    {
                                        col.DefaultValue = Convert.ChangeType(defVal, col.DataType);
                                    }
                                }
                            }
                            else
                            {
                                defVal = defVal.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
                                if (defVal == string.Empty || defVal.ToLower() == "null") continue;

                                col.DefaultValue = Convert.ChangeType(defVal, col.DataType);
                            }
                        }
                    }
                    dr.Close();
                }
                finally
                {
                    cmd.CommandText = string.Format("TRUNCATE TABLE {0}", tempTableName);
                    i = cmd.ExecuteNonQuery();
                    cmd.CommandText = string.Format("DROP TABLE {0}", tempTableName);
                    i = cmd.ExecuteNonQuery();
                }

                // fkTable                               
                cmd.CommandText = @"
SELECT u.constraint_name CONSTRAINT_NAME, u.TABLE_NAME, v.column_name COLUMN_NAME, u.R_TABLE_NAME, u.R_COLUMN_NAME
FROM (SELECT t.table_name TABLE_NAME, s.table_name R_TABLE_NAME, s.column_name R_COLUMN_NAME, t.constraint_name 
      FROM USER_CONSTRAINTS t, USER_CONS_COLUMNS s WHERE t.r_constraint_name = s.constraint_name) u, USER_CONS_COLUMNS v
WHERE u.constraint_name = v.constraint_name";
                new OracleDataAdapter(cmd).Fill(fkTable);

                // utcNow
                cmd.CommandText = "SELECT CURRENT_TIMESTAMP FROM DUAL";
                now = (DateTime)cmd.ExecuteScalar();

                // timeZone
                cmd.CommandText = "SELECT SESSIONTIMEZONE FROM DUAL";
                timeZone = (string)cmd.ExecuteScalar();

                // sequencesTable
                cmd.CommandText = "SELECT SEQUENCE_NAME FROM USER_SEQUENCES";
                new OracleDataAdapter(cmd).Fill(sequencesTable);
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
                string key = row["CONSTRAINT_NAME"].ToString();
                if (!dictionary.ContainsKey(key))
                {
                    dictionary[key] = new List<DataRow>();
                }
                dictionary[key].Add(row);
            }

            foreach (KeyValuePair<string, List<DataRow>> pair in dictionary)
            {
                string constraintName = pair.Key;
                List<DataRow> rows = pair.Value;
                string tableName = rows[0]["TABLE_NAME"].ToString();
                string r_tableName = rows[0]["R_TABLE_NAME"].ToString();

                if (schemaDataSet.Tables[tableName] == null) continue;
                if (schemaDataSet.Tables[r_tableName] == null) continue;

                // distinct
                Dictionary<string, object> columns = new Dictionary<string, object>();
                Dictionary<string, object> r_columns = new Dictionary<string, object>();
                foreach (DataRow row in rows)
                {
                    columns[row["COLUMN_NAME"].ToString()] = null;
                    r_columns[row["R_COLUMN_NAME"].ToString()] = null;
                }
                string[] columnNames = columns.Keys.ToArray();
                string[] r_columnNames = r_columns.Keys.ToArray();

                DataColumn[] fkColumns = new DataColumn[columnNames.Length];
                for (int i = 0; i < columnNames.Length; i++)
                {
                    fkColumns[i] = schemaDataSet.Tables[tableName].Columns[columnNames[i]];
                }

                DataColumn[] pkColumns = new DataColumn[r_columnNames.Length];
                for (int i = 0; i < r_columnNames.Length; i++)
                {
                    pkColumns[i] = schemaDataSet.Tables[r_tableName].Columns[r_columnNames[i]];
                }

                ForeignKeyConstraint fk = new ForeignKeyConstraint(constraintName, pkColumns, fkColumns);
                schemaDataSet.Tables[tableName].Constraints.Add(fk);
            }

            DateTime utcNow = ToUtcDateTime(now, timeZone);
            schemaDataSet.ExtendedProperties["DataSetVersion"] = utcNow.Ticks.ToString();

            schemaDataSet.ExtendedProperties["DatabaseSequences"] = sequencesTable;

            return schemaDataSet;
        }

        protected string GetTemporaryTableName(OracleCommand cmd)
        {
            while (true)
            {
                string name = Guid.NewGuid().ToString("N");
                name = "T" + name.Substring(12);
                cmd.CommandText = string.Format("SELECT TABLE_NAME FROM ALL_TAB_COMMENTS WHERE TABLE_NAME = '{0}'", name);
                object obj = cmd.ExecuteScalar();
                while (obj == null) return name;
            }
        }

        protected override DateTime GetUtcNow()
        {
            string timeZone = (string)ExecuteScalar("SELECT SESSIONTIMEZONE FROM DUAL");
            DateTime now = GetNow();
            return ToUtcDateTime(now, timeZone);
        }

        protected DateTime ToUtcDateTime(DateTime dateTime, string timeZone)
        {
            string xml = Properties.Resources.Timezones;
            XElement xTimezones = XElement.Parse(xml);
            XElement xTimezone = xTimezones.Elements().FirstOrDefault(x => x.Element("Key").Value == timeZone);
            string zone = (xTimezone == null) ? timeZone : xTimezone.Element("Value").Value;

            string[] ss = zone.Split(':');
            int hour = int.Parse(ss[0]);
            int min = 0;
            int sec = 0;
            int msec = 0;
            if (ss.Length > 1) min = int.Parse(ss[1]);
            if (ss.Length > 2) sec = int.Parse(ss[2]);
            if (ss.Length > 3) msec = int.Parse(ss[3]);

            return dateTime.AddHours(-hour).AddMinutes(-min).AddSeconds(-sec).AddMilliseconds(-msec);
        }

        protected override DateTime GetNow()
        {
            //return (DateTime)ExecuteScalar("SELECT SYSDATE FROM DUAL");
            return (DateTime)ExecuteScalar("SELECT CURRENT_TIMESTAMP FROM DUAL");
        }

        //SELECT a.Id, a.User_Id, b.Name 
        //FROM (SELECT NULL Id, 1 User_Id FROM DUAL) a 
        //LEFT JOIN Users b ON a.User_Id = b.Id
        protected override string GenerateGetDefaultSql(XElement query, XElement schema)
        {
            QueryView queryView = new QueryView(query, schema);
            string select = GenerateSelectFields(queryView.Columns);
            string leftJoins = GenerateLeftJoins(queryView.ForeignKeyPaths);

            //
            List<string> list = new List<string>();
            XElement elementSchema = schema.GetElementSchema(query.Name.LocalName);
            foreach (XElement fieldSchema in elementSchema.Elements().Where(p => p.Attribute("Element") == null))
            {
                string column = (fieldSchema.Attribute("Column") == null) ?
                    fieldSchema.Name.LocalName :
                    fieldSchema.Attribute("Column").Value;
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
            string sql = string.Format("SELECT {0} FROM (SELECT {1} FROM DUAL) {2} {3}", select,
               from, queryView.TableAlias, leftJoins);
            return sql;
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
            int skip = int.Parse(query.Element("Skip").Value);
            int take = int.Parse(query.Element("Take").Value);

            string sql = @"
SELECT *
  FROM (SELECT table_alias_sel.*, ROWNUM ROW_NUM 
          FROM ({0}) table_alias_sel
          WHERE ROWNUM <= {1}) table_alias_paging
WHERE table_alias_paging.ROW_NUM >= {2}";

            sql = string.Format(sql, selectSql, skip + take, skip + 1);
            return sql;
        }

        protected override Database.Filter CreateFilter(XElement query, XElement schema)
        {
            return new OracleFilter(query, schema, this);
        }

        protected override object GenerateSequence(string sequenceName)
        {
            string sql = string.Format("SELECT \"{0}\".NEXTVAL FROM DUAL", sequenceName);
            OracleConnection conn = new OracleConnection(Connection.ConnectionString);
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
            return "\"" + tableName + "\"";
        }

        protected override string DecorateColumnName(string columnName)
        {
            return "\"" + columnName + "\"";
        }

        protected override string DecorateStringValue(string value, XElement fieldSchema)
        {
            if (string.IsNullOrEmpty(value)) return "NULL";

            string result = "'" + value.Replace("'", "''") + "'";
            if (fieldSchema.Attribute("SqlDbType").Value.StartsWith("n"))
            {
                result = "N" + result;
            }
            return result;
        }

        protected override string DecorateDateTimeValue(string value, XElement fieldSchema)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";

            string sqlDbType = fieldSchema.Attribute("SqlDbType").Value;
            return DateTime.Parse(value).DecorateDateTimeValue(sqlDbType);
        }

        protected override string DecorateDateTimeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";

            string sValue = DateTime.Parse(value).ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF");

            return string.Format("TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS.FF9')", sValue);
        }

        protected override string DecorateByteArrayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";

            Debug.Assert(value.StartsWith("0x"));

            string result = value.Substring(2);
            return "'" + result + "'";
        }

        protected override string GenerateInsertSql(XElement node, XElement schema, out DbParameter[] parameters)
        {
            XElement elementSchema = schema.GetElementSchema(node.Name.LocalName);
            string tableName = elementSchema.Attribute("Table").Value;

            Dictionary<string, object> fieldValues = GetFieldValues(node, schema);

            string sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", DecorateTableName(tableName),
                string.Join(",", fieldValues.Select(p => DecorateColumnName(p.Key))),
                string.Join(",", fieldValues.Select(p => ":" + p.Key)));

            List<DbParameter> list = new List<DbParameter>();
            foreach (KeyValuePair<string, object> fieldValue in fieldValues)
            {
                DbParameter parameter = CreateDbParameter(":" + fieldValue.Key, fieldValue.Value);
                list.Add(parameter);
            }

            parameters = list.ToArray();
            return sql;
        }

        protected Dictionary<string, object> GetFieldValues(XElement node, XElement schema)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            XElement elementSchema = schema.GetElementSchema(node.Name.LocalName);
            foreach (XElement field in node.Elements())
            {
                XElement fieldSchema = elementSchema.Element(field.Name);

                if (fieldSchema == null) continue;
                if (fieldSchema.Attribute("Element") != null) continue;
                if (fieldSchema.Attribute("ReadOnly").Value == true.ToString()) continue;

                string columnName = fieldSchema.Name.LocalName;
                if (fieldSchema.Attribute("Column") != null)
                {
                    columnName = fieldSchema.Attribute("Column").Value;
                }
                dict.Add(columnName, ToObject(field.Value, fieldSchema));
            }
            return dict;
        }

        protected object ToObject(string value, XElement fieldSchema)
        {
            if (value == string.Empty) return DBNull.Value;
            Type dataType = Type.GetType(fieldSchema.Attribute("DataType").Value);
            if (dataType == typeof(String) || dataType == typeof(Char)) return value;
            if (dataType == typeof(DateTime)) return DateTime.Parse(value);
            if (dataType == typeof(TimeSpan)) return TimeSpan.Parse(value);
            if (IsNumberType(dataType)) return DecorateNumberValue(value, fieldSchema);
            if (dataType == typeof(Boolean)) return bool.Parse(value);
            if (dataType == typeof(Guid)) return Guid.Parse(value);
            if (dataType == typeof(Byte[]))
            {
                // value.StartsWith("0x") 
                int length = value.Length / 2 - 1;
                //foreach() 
            }

            throw new NotSupportedException(dataType.ToString());
        }


    }
}
