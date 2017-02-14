using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.DbObjects
{
    public class TableObject
    {
        public string ElementName { get; private set; }
        public XElement Schema { get; private set; }

        public string TableName { get; private set; }
        public string TableAlias { get; set; }
        
        public IEnumerable<NativeColumn> NativeColumns { get; private set; }
        public IEnumerable<ReferenceColumn> ReferenceColumns { get; private set; }

        protected virtual ElementObject CreateElementObject(string elementName, XElement schema)
        {
            return new ElementObject(elementName, schema);
        }

        public TableObject(string elementName, XElement schema)
        {
            ElementName = elementName;
            Schema = schema;
            TableName = schema.GetElementSchema(elementName).Attribute(Glossary.Table).Value;
            ElementObject elementObject = CreateElementObject(elementName, schema);
            List<NativeColumn> nativeColumnList = new List<NativeColumn>();
            List<ReferenceColumn> referenceColumnList = new List<ReferenceColumn>();
            foreach (Field field in elementObject.Fields)
            {
                if (field is NativeField)
                {
                    nativeColumnList.Add(new NativeColumn(field as NativeField, elementName, schema));
                }
                if (field is ReferenceField)
                {
                    referenceColumnList.Add(new ReferenceColumn(field as ReferenceField, schema));
                }
            }
            NativeColumns = nativeColumnList;
            ReferenceColumns = referenceColumnList;
        }

        private TableObject()
        {
        }    

        internal static TableObject Create(IEnumerable<string> fieldNames, string elementName, XElement schema)
        {
            TableObject tableObj = new TableObject(elementName, schema);
            TableObject tableObject = new TableObject();
            tableObject.ElementName = tableObj.ElementName;
            tableObject.Schema = tableObj.Schema;
            tableObject.TableName = tableObj.TableName;
            tableObject.TableAlias = tableObj.TableAlias;
            List<NativeColumn> nativeColumnList = new List<NativeColumn>();
            foreach (NativeColumn column in tableObj.NativeColumns)
            {
                if (fieldNames.Any(p => p == column.FieldName))
                {
                    nativeColumnList.Add(column);
                }
            }
            List<ReferenceColumn> referenceColumnList = new List<ReferenceColumn>();
            foreach (ReferenceColumn column in tableObj.ReferenceColumns)
            {
                if (fieldNames.Any(p => p == column.FieldName))
                {
                    referenceColumnList.Add(column);
                }
            }
            tableObject.NativeColumns = nativeColumnList;
            tableObject.ReferenceColumns = referenceColumnList;
            return tableObject;
        }
    }

    public abstract class Column
    {
        public string TableAlias { get; set; }
        public string ColumnName { get; protected set; }
        public string FieldName { get; protected set; }
    }

    public class NativeColumn : Column
    {
        protected NativeField NativeField { get; private set; }

        public Type DataType
        {
            get { return NativeField.DataType; }
        }

        public NativeColumn(NativeField field, string elementName, XElement schema)
        {
            NativeField = field;
            FieldName = NativeField.FieldName;
            XElement elementSchema = schema.GetElementSchema(elementName);
            XElement fieldSchema = elementSchema.Element(NativeField.FieldName);
            ColumnName = (fieldSchema.Attribute(Glossary.Column) == null) ?
                FieldName : fieldSchema.Attribute(Glossary.Column).Value;
        }
    }

    public class ReferenceColumn : Column
    {
        protected ReferenceField ReferenceField { get; private set; }

        public Type DataType
        {
            get { return ReferenceField.DataType; }
        }

        public string TableName { get; private set; }

        public ForeignKeyPath ForeignKeyPath { get; private set; }

        public ReferenceColumn(ReferenceField field, XElement schema)
        {
            ReferenceField = field;
            FieldName = ReferenceField.FieldName;

            XElement rElement = schema.GetElementSchema(ReferenceField.Element);
            TableName = rElement.Attribute(Glossary.Table).Value;
            XElement rField = rElement.Element(ReferenceField.Field);
            ColumnName = (rField.Attribute(Glossary.Column) == null) ?
                ReferenceField.Field : rField.Attribute(Glossary.Column).Value;

            ForeignKeyPath = new ForeignKeyPath(ReferenceField.ReferencePath, schema);
        }

    }

}
