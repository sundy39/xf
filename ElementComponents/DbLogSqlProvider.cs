using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;
using XData.Data.Schema;

namespace XData.Data.Components
{
    /*
CREATE TABLE [dbo].[DbLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ElementName] [nvarchar](50) NOT NULL,
	[TableName] [nvarchar](50) NOT NULL,
	[KeyValue] [nvarchar](50) NOT NULL,
	[Manipulation] [nvarchar](50) NOT NULL,
	[Deleted] [nvarchar](max) NULL,
	[Inserted] [nvarchar](max) NULL,
	[ElementSchema] [nvarchar](max) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedUserId] [int] NULL,
	[CreatedUserName] [nvarchar](50) NULL,
 CONSTRAINT [PK_DbLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE [dbo].[DbLog] ADD  CONSTRAINT [DF_DbLog_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
GO      
     */
    public class DbLogSqlProvider
    {
        public enum Manipulation { Insert, Delete, Update }

        public const string USER_ID_FIELD_NAME = "Id";
        public const string USER_NAME_FIELD_NAME = "UserName";

        private string _userIdFieldName = USER_ID_FIELD_NAME;
        public string UserIdFieldName
        {
            get { return _userIdFieldName; }
            set { _userIdFieldName = value; }
        }

        private string _userNameFieldName = USER_NAME_FIELD_NAME;
        public string UserNameFieldName
        {
            get { return _userNameFieldName; }
            set { _userNameFieldName = value; }
        }

        internal protected ElementContext ElementContext { get; internal set; }

        private ElementQuerier _elementQuerier = null;
        protected ElementQuerier ElementQuerier
        {
            get
            {
                if (_elementQuerier == null)
                {
                    _elementQuerier = new ElementQuerier(ElementContext);
                }
                return _elementQuerier;
            }
        }

        protected virtual string GenerateSql(XElement node, XElement schema, Manipulation manipulation, XElement user)
        {
            XElement dbLogEntrySchema = schema.GetElementSchemaBySetName("DbLog");
            if (dbLogEntrySchema == null) return null;

            string dbLogTableName = dbLogEntrySchema.Attribute("Table").Value;

            XElement elementSchema = schema.GetElementSchema(node.Name.LocalName);
            string tableName = elementSchema.Attribute("Table").Value;
            string[] key = elementSchema.GetKeySchema().ExtractKey(node).Elements().Select(x => x.Value).ToArray();
            string keyValue = string.Join(",", key);

            string deleted = null;
            string inserted = null;
            if (manipulation == Manipulation.Insert || manipulation == Manipulation.Update)
            {
                inserted = node.ToString();
            }
            if (manipulation == Manipulation.Delete || manipulation == Manipulation.Update)
            {
                XElement original = ElementQuerier.Find(node.Name.LocalName, key, null, schema);
                deleted = original.ToString();
            }

            string userId = null;
            string usersName = null;
            if (user != null)
            {
                userId = user.Element(USER_ID_FIELD_NAME).Value;
                usersName = user.Element(USER_NAME_FIELD_NAME).Value;
            }

            string format = @"
INSERT INTO DbLog
(TableName,KeyValue,Manipulation,Deleted,Inserted,ElementSchema,CreatedUserId,CreatedUserName)
VALUES
('{0}','{1}','{2}',{3},{4},'{5}',{6},{7});";

            string sql = string.Format(format, tableName, keyValue, manipulation.ToString(),
                (deleted == null) ? "null" : "'" + deleted + "'",
                (inserted == null) ? "null" : "'" + inserted + "'",
                elementSchema.ToString(),
                (userId == null) ? "null" : "'" + userId + "'",
                (usersName == null) ? "null" : "'" + usersName + "'");

            return sql;
        }

        public virtual string GenerateSqlOnInserted(XElement node, XElement schema, XElement user)
        {
            return GenerateSql(node, schema, Manipulation.Insert, user);
        }

        public virtual string GenerateSqlOnDeleting(XElement node, XElement schema, XElement user)
        {
            return GenerateSql(node, schema, Manipulation.Delete, user);
        }

        public virtual string GenerateSqlOnUpdating(XElement node, XElement schema, XElement user)
        {
            return GenerateSql(node, schema, Manipulation.Update, user);
        }


    }
}
