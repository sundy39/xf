using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.DbObjects
{
    public class ForeignKey
    {
        public string TableAlias { get; set; }
        public string RelatedTableAlias { get; set; }

        public string Table { get; private set; }
        public string[] Columns { get; private set; }
        public string RelatedTable { get; private set; }
        public string[] RelatedColumns { get; private set; }

        public string Content { get; private set; }

        public ForeignKey(ManyToOneRelationship relationship, XElement schema)
        {
            Initialize(relationship, schema);
        }

        public ForeignKey(OneToOneRelationship relationship, XElement schema)
        {
            Initialize(relationship, schema);
        }

        private void Initialize(SimpleRelationship relationship, XElement schema)
        {
            XElement elementSchema = schema.GetElementSchema(relationship.ElementName);
            Table = elementSchema.Attribute(Glossary.Table).Value;
            Columns = new string[relationship.FieldNames.Length];
            for (int i = 0; i < Columns.Length; i++)
            {
                XAttribute attr = elementSchema.Element(relationship.FieldNames[i]).Attribute(Glossary.Column);
                Columns[i] = (attr == null) ? relationship.FieldNames[i] : attr.Value;
            }

            XElement relatedSchema = schema.GetElementSchema(relationship.RelatedElementName);
            RelatedTable = relatedSchema.Attribute(Glossary.Table).Value;
            RelatedColumns = new string[relationship.RelatedFieldNames.Length];
            for (int i = 0; i < RelatedColumns.Length; i++)
            {
                XAttribute attr = relatedSchema.Element(relationship.RelatedFieldNames[i]).Attribute(Glossary.Column);
                RelatedColumns[i] = (attr == null) ? relationship.RelatedFieldNames[i] : attr.Value;
            }

            Content = string.Format("{0}({1}),{2}({3})", Table, Columns.Aggregate((p, v) => string.Format("{0},{1}", p, v)),
                RelatedTable, RelatedColumns.Aggregate((p, v) => string.Format("{0},{1}", p, v)));
        }
    }

    public class ForeignKeyPath
    {
        public ForeignKey[] ForeignKeys { get; private set; }
        public string Content { get; private set; }

        public ForeignKeyPath(ReferencePath referencePath, XElement schema)
        {
            List<ForeignKey> foreignKeys = new List<ForeignKey>();
            foreach (ManyToOneRelationship relationship in referencePath.ManyToOneRelationships)
            {
                foreignKeys.Add(new ForeignKey(relationship, schema));
            }
            ForeignKeys = foreignKeys.ToArray();
            Content = ForeignKeys.Select(p => p.Content).Aggregate((p, v) => string.Format("{0};{1}", p, v));
        }

    }
}
