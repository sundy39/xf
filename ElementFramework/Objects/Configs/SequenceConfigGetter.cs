using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Objects
{
    public class SequenceConfigGetter : DatabaseConfigGetter
    {
        // ".*{0}.*{1}.*", TableName, ColumnName
        protected string Format { get; private set; }

        public SequenceConfigGetter(string format)
        {
            Format = format;
        }

        public override XElement GetDatabaseConfig()
        {
            XElement config = new XElement("Schema");
            IEnumerable<XElement> sequenceSchemas = DatabaseSchema.Elements("Sequence").Where(p => p.Attribute("SequenceName") != null);
            IEnumerable<XElement> elementSchemas = DatabaseSchema.Elements().Where(x => x.Attribute(Glossary.Set) != null);
            foreach (XElement elementSchema in elementSchemas)
            {
                foreach (XElement fieldSchema in elementSchema.Elements())
                {
                    XAttribute attr = fieldSchema.Attribute("AutoIncrement");
                    if (attr != null && attr.Value == true.ToString()) continue;             

                    string tableName = elementSchema.Attribute(Glossary.Table).Value;
                    string columnName = (fieldSchema.Attribute(Glossary.Column) == null) ? fieldSchema.Name.LocalName : fieldSchema.Attribute(Glossary.Column).Value;
                    string sequenceName = GetSequenceName(tableName, columnName);

                    if (!string.IsNullOrWhiteSpace(sequenceName))
                    {
                        if (sequenceSchemas.Any(p => p.Attribute("SequenceName").Value == sequenceName))
                        {
                            XElement attrElmt = new XElement("Sequence");
                            attrElmt.Add(new XElement("SequenceName", sequenceName));
                            XElement fieldConfig = new XElement(fieldSchema.Name);
                            fieldConfig.Add(attrElmt);
                            XElement elementConfig = new XElement(elementSchema.Name);
                            elementConfig.SetAttributeValue("Set", elementSchema.Attribute("Set").Value);
                            elementConfig.Add(fieldConfig);
                            config.Add(elementConfig);
                        }
                    }
                }
            }
            return config;
        }

        protected string GetSequenceName(string tableName, string columnName)
        {
            if (Format.Contains("{1}"))
            {
                return string.Format(Format, tableName, columnName);
            }
            XElement elementSchema = DatabaseSchema.Elements().First(x => x.Attribute(Glossary.Table).Value == tableName);
            string primaryKey = (elementSchema.Attribute("PrimaryKey") == null) ? string.Empty : elementSchema.Attribute("PrimaryKey").Value;
            string[] keyColumns = primaryKey.Split(',');
            if (keyColumns.Length == 1)
            {
                if (keyColumns[0] == columnName)
                {
                    return string.Format(Format, tableName);
                }
            }
            return null;
        }


    }
}
