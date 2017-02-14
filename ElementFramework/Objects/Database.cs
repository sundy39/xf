using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Objects
{
    public abstract partial class Database
    {
        protected abstract DbConnection CreateConnection(string connectionString);
        protected abstract DbDataAdapter CreateDbDataAdapter();
        public abstract DbParameter CreateDbParameter(string parameterName, object obj);
        protected abstract DataSet CreateSchemaDataSet();
        internal protected abstract DateTime GetUtcNow();
        internal protected abstract DateTime GetNow();

        public DbConnection Connection { get; private set; }
        public DbTransaction Transaction { get; set; }

        internal protected readonly string DatabaseVersion;

        protected readonly static Dictionary<string, DataSet> SchemaDataSets = new Dictionary<string, DataSet>();

        public virtual int ExecuteSqlCommand(string sql, params object[] parameters)
        {
            DbCommand cmd = Connection.CreateCommand();
            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            ConnectionState state = cmd.Connection.State;
            try
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
        }

        public IEnumerable<XElement> SqlQuery(XElement schema, string elementName, string sql, params object[] parameters)
        {
            if (schema.Attribute("Ex") == null)
            {
                return SqlQuery(elementName, sql, parameters);
            }
            else
            {
                return SqlQueryEx(schema, elementName, sql, parameters);
            }
        }

        public IEnumerable<XElement> SqlQuery(string elementName, string sql, params object[] parameters)
        {
            DataTable dataTable = CreateDataTable(sql, parameters);
            return dataTable.ToElements(elementName);
        }

        public IEnumerable<XElement> SqlQueryEx(XElement schema, string elementName, string sql, params object[] parameters)
        {
            DataTable dataTable = CreateDataTable(sql, parameters);
            return dataTable.ToElementsEx(elementName, schema);
        }

        protected virtual DataTable CreateDataTable(string sql, params object[] parameters)
        {
            DataTable dataTable = new DataTable();
            DbCommand cmd = Connection.CreateCommand();
            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            DbDataAdapter da = CreateDbDataAdapter();
            da.SelectCommand = cmd;
            ConnectionState state = cmd.Connection.State;
            try
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }
                int i = da.Fill(dataTable);
                return dataTable;
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
        }

        protected virtual object ExecuteScalar(string sql)
        {
            DbCommand cmd = Connection.CreateCommand();
            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.CommandText = sql;
            ConnectionState state = cmd.Connection.State;
            try
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }
                return cmd.ExecuteScalar();
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
        }

        internal protected DataSet GetSchemaDataSet()
        {
            string databaseKey = string.Format("Data Source={0};Initial Catalog={1};ProviderName={2}", Connection.DataSource, Connection.Database, Connection.GetType().Namespace);

            if (SchemaDataSets.ContainsKey(databaseKey))
            {
                if (SchemaDataSets[databaseKey].ExtendedProperties[Glossary.DatabaseVersion].ToString() == DatabaseVersion)
                {
                    return SchemaDataSets[databaseKey];
                }
            }

            lock (SchemaDataSets)
            {
                DataSet databaseSchema = CreateSchemaDataSet();
                databaseSchema.ExtendedProperties[Glossary.DatabaseKey] = databaseKey;
                databaseSchema.ExtendedProperties[Glossary.DatabaseVersion] = DatabaseVersion;
                SchemaDataSets[databaseKey] = databaseSchema;
            }

            return SchemaDataSets[databaseKey];
        }

        public Database(string connectionString, string databaseVersion)
        {
            Connection = CreateConnection(connectionString);
            DatabaseVersion = databaseVersion;
        }

        #region Decorate

        protected virtual string DecorateTableName(string tableName)
        {
            return tableName;
        }

        protected virtual string DecorateColumnName(string columnName)
        {
            return columnName;
        }

        protected string DecorateValue(string value, XElement fieldSchema)
        {
            Type dataType = Type.GetType(fieldSchema.Attribute("DataType").Value);
            if (dataType == typeof(String) || dataType == typeof(Char)) return DecorateStringValue(value, fieldSchema);
            if (dataType == typeof(DateTime)) return DecorateDateTimeValue(value, fieldSchema);
            if (dataType == typeof(TimeSpan)) return DecorateTimeSpanValue(value, fieldSchema);
            if (IsNumberType(dataType)) return DecorateNumberValue(value, fieldSchema);
            if (dataType == typeof(Boolean)) return DecorateBooleanValue(value, fieldSchema);
            if (dataType == typeof(Guid)) return DecorateGuidValue(value, fieldSchema);
            if (dataType == typeof(Byte[])) return DecorateByteArrayValue(value, fieldSchema);

            throw new NotSupportedException(dataType.ToString());
        }

        // Oracle must override.
        // SQL Server must override.
        protected virtual string DecorateStringValue(string value, XElement fieldSchema)
        {
            if (string.IsNullOrEmpty(value)) return DecorateEmptyStringValue(fieldSchema);
            return DecorateStringValue(value);
        }

        protected virtual string DecorateEmptyStringValue(XElement fieldSchema)
        {
            XElement displayFormatAttribute = fieldSchema.Element("DisplayFormat");
            if (displayFormatAttribute == null)
            {
                XElement requiredAttribute = fieldSchema.Element("Required");
                if (requiredAttribute == null)
                {
                    bool allowDBNull = bool.Parse(fieldSchema.Attribute("AllowDBNull").Value.ToLower());
                    if (allowDBNull)
                    {
                        if (fieldSchema.Element("DefaultValue") == null)
                        {
                            if (fieldSchema.Attribute("DefaultValue") == null)
                            {
                                return "NULL";
                            }
                        }
                        return "''";
                    }
                    else
                    {
                        return "''";
                    }
                }
                else
                {
                    //When you set AllowEmptyStrings to true for a data field, Dynamic Data does not perform validation and transforms the empty string to a null value.This value is then passed to the database.
                    //If the database does not allow null values, it raises an error.To avoid this error, you must also set the ConvertEmptyStringToNull to false.
                    bool allowEmptyStrings = false;
                    if (requiredAttribute.Element("AllowEmptyStrings") != null)
                    {
                        allowEmptyStrings = bool.Parse(requiredAttribute.Element("AllowEmptyStrings").Value.ToLower());
                    }
                    return allowEmptyStrings ? "NULL" : "''";
                }
            }
            else
            {
                bool convertEmptyStringToNull = true;
                if (displayFormatAttribute.Element("ConvertEmptyStringToNull") != null)
                {
                    convertEmptyStringToNull = bool.Parse(displayFormatAttribute.Element("ConvertEmptyStringToNull").Value.ToLower());
                }
                return convertEmptyStringToNull ? "NULL" : "''";
            }
        }

        protected virtual string DecorateNumberValue(string value, XElement fieldSchema)
        {
            return DecorateNumberValue(value);
        }

        protected virtual string DecorateDateTimeValue(string value, XElement fieldSchema)
        {
            return DecorateDateTimeValue(value);
        }

        protected virtual string DecorateTimeSpanValue(string value, XElement fieldSchema)
        {
            return DecorateTimeSpanValue(value);
        }

        protected virtual string DecorateBooleanValue(string value, XElement fieldSchema)
        {
            return DecorateBooleanValue(value);
        }

        protected virtual string DecorateGuidValue(string value, XElement fieldSchema)
        {
            return DecorateGuidValue(value);
        }

        protected virtual string DecorateByteArrayValue(string value, XElement fieldSchema)
        {
            return DecorateByteArrayValue(value);
        }

        protected string DecorateValue(string value, Type dataType)
        {
            if (dataType == typeof(String) || dataType == typeof(Char)) return DecorateStringValue(value);
            if (dataType == typeof(DateTime)) return DecorateDateTimeValue(value);
            if (dataType == typeof(TimeSpan)) return DecorateTimeSpanValue(value);
            if (IsNumberType(dataType)) return DecorateNumberValue(value);
            if (dataType == typeof(Boolean)) return DecorateBooleanValue(value);
            if (dataType == typeof(Guid)) return DecorateGuidValue(value);
            if (dataType == typeof(Byte[])) return DecorateByteArrayValue(value);

            throw new NotSupportedException(dataType.ToString());
        }

        protected virtual string DecorateStringValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "NULL";
            return "'" + value.Replace("'", "''") + "'";
        }

        protected virtual string DecorateNumberValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";
            double.Parse(value); // Anti SQL-injection
            return value;
        }

        protected virtual string DecorateDateTimeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";
            return "'" + DateTime.Parse(value).ToCanonicalString() + "'";
        }

        protected virtual string DecorateTimeSpanValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";

            string result = TimeSpan.Parse(value).ToString();
            return "'" + result + "'";
        }

        protected virtual string DecorateBooleanValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";
            int? nullableInt = null;
            if (value.ToLower() == bool.TrueString.ToLower())
            {
                nullableInt = 1;
            }
            else if (value.ToLower() == bool.FalseString.ToLower())
            {
                nullableInt = 0;
            }
            if (nullableInt == null) throw new ArgumentException(string.Format(Messages.Is_Not_Boolean, value), "value");
            return nullableInt.ToString();
        }

        protected virtual string DecorateGuidValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";
            Guid.Parse(value); // Anti SQL-injection
            return "'" + value + "'";
        }

        protected virtual string DecorateByteArrayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "NULL";

            // Anti SQL-injection
            string hexadecimal = value.Substring(2).ToUpper();
            for (int i = 0; i < hexadecimal.Length; i++)
            {
                char c = hexadecimal[i];
                if (c >= '0' && c <= '9') continue;
                if (c >= 'A' && c <= 'F') continue;
                throw new SqlInjectionException();
            }
            return value;
        }

        protected static bool IsNumberType(Type type)
        {
            return (type == typeof(SByte) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64)
                || type == typeof(Byte) || type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64)
                || type == typeof(Decimal) || type == typeof(Single) || type == typeof(Double));
        }

        #endregion


    }
}
