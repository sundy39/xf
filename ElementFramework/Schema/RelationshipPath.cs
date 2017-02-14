using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema.Validation;

namespace XData.Data.Schema
{
    public class RelationshipPath
    {
        public string From { get; protected set; }
        public string To { get; protected set; }
        public string Content { get; protected set; }

        public SimpleRelationship[] Relationships { get; protected set; }

        protected RelationshipPath(XElement path)
        {
            From = path.Attribute(Glossary.RelationshipFrom).Value;
            To = path.Attribute(Glossary.RelationshipTo).Value;
            Content = path.Attribute(Glossary.RelationshipContent).Value;
        }

        protected void Validate()
        {
            for (int i = 0; i < Relationships.Length; i++)
            {
                if (i < Relationships.Length - 1)
                {
                    if (Relationships[i].RelatedElementName != Relationships[i + 1].ElementName)
                    {
                        throw new SchemaValidationException(Messages.Relationships_Non_Connected, new string[] { Content });
                    }
                }
            }
        }

        protected string[] GetFromTo()
        {
            string[] fromTo = new string[2];
            fromTo[0] = Relationships[0].ElementName;
            fromTo[1] = Relationships[Relationships.Length - 1].RelatedElementName;
            return fromTo;
        }

        protected RelationshipPath()
        {
        }

        public RelationshipPath(Relationship relationship)
        {
            From = relationship.From;
            To = relationship.To;
            Content = relationship.Content;
            if (relationship is ManyToManyRelationship)
            {
                ManyToManyRelationship manyToManyRelationship = relationship as ManyToManyRelationship;
                Relationships = new SimpleRelationship[] { manyToManyRelationship.OneToManyRelationship, manyToManyRelationship.ManyToOneRelationship };
            }
            else
            {
                Relationships = new SimpleRelationship[] { (SimpleRelationship)relationship };
            }

        }

        public RelationshipPath(SimpleRelationship[] relationships)
        {
            Relationships = relationships;
            Content = Relationships.Select(p => p.Content).Aggregate((p, v) => string.Format("{0};{1}", p, v));
            string[] fromTo = GetFromTo();
            From = fromTo[0];
            To = fromTo[1];

            //
            Validate();
        }

        public RelationshipPath Reverse()
        {
            RelationshipPath path = new RelationshipPath();
            List<SimpleRelationship> list = new List<SimpleRelationship>();
            foreach (SimpleRelationship relationship in Relationships)
            {
                list.Add((SimpleRelationship)relationship.Reverse());
            }
            list.Reverse();
            path.Relationships = list.ToArray();

            path.From = path.Relationships[0].ElementName;
            path.To = path.Relationships[path.Relationships.Length - 1].RelatedElementName;
            path.Content = path.Relationships.Select(p => p.Content).Aggregate((p, v) => string.Format("{0};{1}", p, v));
            return path;
        }

        // OneToManyRelationships
        internal protected RelationshipPath(string content)
        {
            Content = content;
            InitializeRelationships();
            string[] fromTo = GetFromTo();
            From = fromTo[0];
            To = fromTo[1];

            //
            Validate();
        }

        protected void InitializeRelationships()
        {
            List<OneToManyRelationship> relationships = new List<OneToManyRelationship>();
            string[] contents = Content.Split(';');
            for (int i = 0; i < contents.Length; i++)
            {
                OneToManyRelationship relationship = new OneToManyRelationship(contents[i]);
            }
            Relationships = relationships.ToArray();
        }
    }

    //<ReferencePath FromTo="" Content="" Name="" /> 
    public class ReferencePath : RelationshipPath
    {
        public ManyToOneRelationship[] ManyToOneRelationships { get; private set; }

        public ReferencePath(XElement path)
            : base(path)
        {
            InitializeManyToOneRelationships();
            Relationships = ManyToOneRelationships;

            //
            string[] fromTo = GetFromTo();
            if (From != fromTo[0] || To != fromTo[1])
            {
                throw new SchemaValidationException(Messages.FromTo_Not_Match_Content, new string[] { Content.ToString() });
            }

            //
            Validate();
        }

        public ReferencePath(string content)
            : base()
        {
            Content = content;
            InitializeManyToOneRelationships();
            Relationships = ManyToOneRelationships;
            string[] fromTo = GetFromTo();
            From = fromTo[0];
            To = fromTo[1];

            //
            Validate();
        }

        private void InitializeManyToOneRelationships()
        {
            List<ManyToOneRelationship> relationships = new List<ManyToOneRelationship>();
            string[] contents = Content.Split(';');
            for (int i = 0; i < contents.Length; i++)
            {
                ManyToOneRelationship relationship = new ManyToOneRelationship(contents[i]);
                relationships.Add(relationship);
            }
            ManyToOneRelationships = relationships.ToArray();
        }

        public ReferencePath(ManyToOneRelationship manyToOneRelationship)
            : base(manyToOneRelationship)
        {
            ManyToOneRelationships = new ManyToOneRelationship[] { manyToOneRelationship };
        }

        public ReferencePath(OneToOneRelationship oneToOneRelationship)
            : base(oneToOneRelationship)
        {
            XElement schema = oneToOneRelationship.ToElement();
            schema.SetAttributeValue(Glossary.RelationshipType, "ManyToOne");
            ManyToOneRelationships = new ManyToOneRelationship[] { new ManyToOneRelationship(schema) };
        }

    }

    public static class RelationshipPathExtensions
    {
        internal static bool IsValid(this RelationshipPath relationshipPath, XElement schema)
        {
            for (int i = 0; i < relationshipPath.Relationships.Length; i++)
            {
                if (!relationshipPath.Relationships[i].IsValid(schema)) return false;
            }
            return true;
        }

    }
}
