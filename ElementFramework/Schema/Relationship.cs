using System;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema.Validation;

namespace XData.Data.Schema
{
    public abstract class Relationship
    {
        public string RelationshipType
        {
            get
            {
                Type type = this.GetType();
                string typeName = type.Name.Substring(0, type.Name.Length - Glossary.Relationship.Length);
                return typeName;
            }
        }

        public string From { get; protected set; }
        public string To { get; protected set; }
        public string Content { get; protected set; }

        protected Relationship(XElement relationship)
        {
            From = relationship.Attribute(Glossary.RelationshipFrom).Value;
            To = relationship.Attribute(Glossary.RelationshipTo).Value;
            Content = relationship.Attribute(Glossary.RelationshipContent).Value;
        }

        protected Relationship()
        {
        }

        public XElement ToElement()
        {
            Type type = this.GetType();
            string relationshipType = null;
            if (type.Name.StartsWith("ManyToOne"))
            {
                relationshipType = "ManyToOne";
            }
            else if (type.Name.StartsWith("OneToMany"))
            {
                relationshipType = "OneToMany";
            }
            else if (type.Name.StartsWith("OneToOne"))
            {
                relationshipType = "OneToOne";
            }
            else if (type.Name.StartsWith("ManyToMany"))
            {
                relationshipType = "ManyToMany";
            }
            //else SimpleRelationship

            XElement relationship = new XElement(Glossary.Relationship);
            relationship.SetAttributeValue(Glossary.RelationshipFrom, From);
            relationship.SetAttributeValue(Glossary.RelationshipTo, To);
            if (!string.IsNullOrWhiteSpace(relationshipType))
            {
                // SimpleRelationship
                relationship.SetAttributeValue(Glossary.RelationshipType, relationshipType);
            }
            relationship.SetAttributeValue(Glossary.RelationshipContent, Content);
            return relationship;
        }

        public abstract Relationship Reverse();

    }

    public abstract class SimpleRelationship : Relationship
    {
        public string ElementName { get; protected set; }
        public string[] FieldNames { get; protected set; }
        public string RelatedElementName { get; protected set; }
        public string[] RelatedFieldNames { get; protected set; }

        protected SimpleRelationship(XElement relationship)
            : base(relationship)
        {
            if (relationship.Attribute(Glossary.RelationshipType).Value == "ManyToMany") throw new NotSupportedException("ManyToMany");
            Type type = this.GetType();
            string relationshipType = relationship.Attribute(Glossary.RelationshipType).Value;
            if (relationshipType != type.Name.Substring(0, type.Name.Length - Glossary.Relationship.Length))
            {
                throw new SchemaValidationException(string.Format(Messages.Type_Not_Match_Relationship, relationship.ToString()), new string[] { relationship.ToString() });
            }

            Initialize();

            if (From != ElementName || To != RelatedElementName)
            {
                throw new SchemaValidationException(Messages.FromTo_Not_Match_Content, new string[] { relationship.ToString() });
            }
        }

        protected void Initialize()
        {
            if (Content.IndexOf(';') != -1) throw new SchemaValidationException(string.Format(Messages.Not_SimpleRelationship, Content), new string[] { Content });

            int index = Content.Trim().IndexOf("),");
            string elementStr = Content.Substring(0, index + 1).Trim();
            string relatedStr = Content.Substring(index + 2).Trim();

            string[] fieldNames;
            ElementName = GetElementName(elementStr, out fieldNames);
            FieldNames = fieldNames;

            string[] relatedFieldNames;
            RelatedElementName = GetElementName(relatedStr, out relatedFieldNames);
            RelatedFieldNames = relatedFieldNames;
        }

        protected SimpleRelationship(string content)
            : base()
        {
            Content = content;
            Initialize();
            From = ElementName;
            To = RelatedElementName;
        }

        private string GetElementName(string str, out string[] fieldNames)
        {
            int index = str.IndexOf('(');
            string result = str.Substring(0, index);
            fieldNames = str.Substring(index + 1).TrimEnd(')').Split(',');
            return result;
        }

        public override Relationship Reverse()
        {
            string reverseTypeName;
            switch (RelationshipType)
            {
                case "ManyToOne":
                    reverseTypeName = "OneToMany";
                    break;
                case "OneToMany":
                    reverseTypeName = "ManyToOne";
                    break;
                case "OneToOne":
                    reverseTypeName = "OneToOne";
                    break;
                default:
                    throw new NotSupportedException(RelationshipType);
            }
            XElement relationship = new XElement(Glossary.Relationship);
            relationship.SetAttributeValue(Glossary.RelationshipFrom, RelatedElementName);
            relationship.SetAttributeValue(Glossary.RelationshipTo, ElementName);
            relationship.SetAttributeValue(Glossary.RelationshipType, reverseTypeName);
            relationship.SetAttributeValue(Glossary.RelationshipContent,
                string.Format("{0}({1}),{2}({3})",
                RelatedElementName,
                RelatedFieldNames.Aggregate((p, v) => string.Format("{0},{1}", p, v)),
                ElementName,
                FieldNames.Aggregate((p, v) => string.Format("{0},{1}", p, v)), ""
                ));
            switch (reverseTypeName)
            {
                case "ManyToOne":
                    return new ManyToOneRelationship(relationship);
                case "OneToMany":
                    return new OneToManyRelationship(relationship);
                case "OneToOne":
                    return new OneToOneRelationship(relationship);
                default:
                    throw new NotSupportedException(reverseTypeName);
            }
        }
    }

    public class ManyToOneRelationship : SimpleRelationship
    {
        public ManyToOneRelationship(XElement manyToOneRelationship)
            : base(manyToOneRelationship)
        {
        }

        public ManyToOneRelationship(string content)
            : base(content)
        {
        }
    }

    public class OneToManyRelationship : SimpleRelationship
    {
        public OneToManyRelationship(XElement oneToManyRelationship)
            : base(oneToManyRelationship)
        {
        }

        public OneToManyRelationship(string content)
            : base(content)
        {
        }
    }

    public class OneToOneRelationship : SimpleRelationship
    {
        public OneToOneRelationship(XElement oneToOneRelationship)
            : base(oneToOneRelationship)
        {
        }

        public OneToOneRelationship(string content)
            : base(content)
        {
        }
    }

    //<Relationship FromTo="User,Role" Type="ManyToMany" Content="User(Id),UserInRole(User_Id);UserInRole(Role_Id),Role(Id)" />
    public class ManyToManyRelationship : Relationship
    {
        public OneToManyRelationship OneToManyRelationship { get; private set; }
        public ManyToOneRelationship ManyToOneRelationship { get; private set; }

        public ManyToManyRelationship(XElement manyToManyRelationship)
            : base(manyToManyRelationship)
        {
            string relationshipType = manyToManyRelationship.Attribute(Glossary.RelationshipType).Value;
            if (relationshipType != "ManyToMany") throw new NotSupportedException(relationshipType);

            SetRelationships();

            if (From != OneToManyRelationship.ElementName || To != ManyToOneRelationship.RelatedElementName)
            {
                throw new SchemaValidationException(Messages.FromTo_Not_Match_Content, new string[] { manyToManyRelationship.ToString() });
            }

            Validate();
        }

        private void SetRelationships()
        {
            XElement oneToManyRelationship = new XElement(Glossary.Relationship);
            XElement manyToOneRelationship = new XElement(Glossary.Relationship);
            string[] contents = Content.Split(';');
            contents[0] = contents[0].Trim();
            contents[1] = contents[1].Trim();

            string[] fromTo = GetSimpleRelationshipFromTo(contents[0]);
            oneToManyRelationship.SetAttributeValue(Glossary.RelationshipFrom, fromTo[0]);
            oneToManyRelationship.SetAttributeValue(Glossary.RelationshipTo, fromTo[1]);
            oneToManyRelationship.SetAttributeValue(Glossary.RelationshipType, "OneToMany");
            oneToManyRelationship.SetAttributeValue(Glossary.RelationshipContent, contents[0]);

            fromTo = GetSimpleRelationshipFromTo(contents[1]);
            manyToOneRelationship.SetAttributeValue(Glossary.RelationshipFrom, fromTo[0]);
            manyToOneRelationship.SetAttributeValue(Glossary.RelationshipTo, fromTo[1]);
            manyToOneRelationship.SetAttributeValue(Glossary.RelationshipType, "ManyToOne");
            manyToOneRelationship.SetAttributeValue(Glossary.RelationshipContent, contents[1]);
            OneToManyRelationship = new OneToManyRelationship(oneToManyRelationship);
            ManyToOneRelationship = new ManyToOneRelationship(manyToOneRelationship);
        }

        public ManyToManyRelationship(string content)
            : base()
        {
            Content = content;
            SetRelationships();
            From = OneToManyRelationship.ElementName;
            To = ManyToOneRelationship.RelatedElementName;

            Validate();
        }

        public ManyToManyRelationship(OneToManyRelationship oneToManyRelationship, ManyToOneRelationship manyToOneRelationship)
            : base()
        {
            OneToManyRelationship = oneToManyRelationship;
            ManyToOneRelationship = manyToOneRelationship;
            From = oneToManyRelationship.ElementName;
            To = manyToOneRelationship.RelatedElementName;
            Content = string.Format("{0};{1}", oneToManyRelationship.Content, manyToOneRelationship.Content);

            Validate();
        }

        private void Validate()
        {
            if (OneToManyRelationship.RelatedElementName != ManyToOneRelationship.ElementName)
            {
                throw new SchemaValidationException(Messages.Relationships_Non_Connected, new string[] { Content.ToString() });
            }
        }

        private string[] GetSimpleRelationshipFromTo(string content)
        {
            int index = content.IndexOf("),");
            string elementStr = content.Substring(0, index + 1).Trim();
            string relatedStr = content.Substring(index + 2).Trim();

            index = elementStr.IndexOf('(');
            string from = elementStr.Substring(0, index).Trim();
            index = relatedStr.IndexOf('(');
            string to = relatedStr.Substring(0, index).Trim();
            return new string[] { from, to };
        }

        public override Relationship Reverse()
        {
            return new ManyToManyRelationship(
                ManyToOneRelationship.Reverse() as OneToManyRelationship,
                OneToManyRelationship.Reverse() as ManyToOneRelationship);
        }
    }

    internal static class ManyToManyRelationshipExtensions
    {
        internal static XElement CreateManyToManyElement(this ManyToManyRelationship manyToManyRelationship, XElement from, XElement to)
        {
            var oneToManyRelationship = manyToManyRelationship.OneToManyRelationship;
            var manyToOneRelationship = manyToManyRelationship.ManyToOneRelationship;
            XElement node = new XElement(oneToManyRelationship.RelatedElementName);
            for (int i = 0; i < oneToManyRelationship.FieldNames.Length; i++)
            {
                node.SetElementValue(oneToManyRelationship.RelatedFieldNames[i],
                   from.Element(oneToManyRelationship.FieldNames[i]).Value);
            }
            for (int i = 0; i < manyToOneRelationship.FieldNames.Length; i++)
            {
                node.SetElementValue(manyToOneRelationship.FieldNames[i],
                  to.Element(manyToOneRelationship.RelatedFieldNames[i]).Value);
            }
            return node;
        }

    }
}
