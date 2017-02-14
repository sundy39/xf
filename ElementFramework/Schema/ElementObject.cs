using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Schema
{
    public class ElementObject
    {
        public string ElementName { get; protected set; }
        public XElement Schema { get; protected set; }
        public IEnumerable<Field> Fields { get; protected set; }

        private IEnumerable<RelatedObject> _objects = null;
        public IEnumerable<RelatedObject> Objects
        {
            get
            {
                if (_objects == null)
                {
                    _objects = Schema.GetRelatedObjects(ElementName);
                }
                return _objects;
            }
        }

        private IEnumerable<string> _elementAsParentFields = null;
        public IEnumerable<string> ElementAsParentFields
        {
            get
            {
                if (_elementAsParentFields == null)
                {
                    _elementAsParentFields = Objects.GetElementAsParentFields();
                }
                return _elementAsParentFields;
            }
        }

        public ElementObject(string elementName, XElement schema)
        {
            ElementName = elementName;
            Schema = schema;
            XElement elementSchema = Schema.GetElementSchema(elementName);
            List<Field> fieldList = new List<Field>();
            foreach (XElement fieldSchema in elementSchema.Elements())
            {
                if (fieldSchema.Attribute(Glossary.Element) == null)
                {
                    fieldList.Add(new NativeField(fieldSchema));
                }
                else
                {
                    if (fieldSchema.Attribute(Glossary.Field) != null)
                    {
                        fieldList.Add(new ReferenceField(fieldSchema, elementName, schema));
                    }
                }
            }
            Fields = fieldList;
        }


    }

    public abstract class Field
    {
        public string FieldName { get; protected set; }
        public Type DataType { get; protected set; }

        protected Field(XElement fieldSchema)
        {
            FieldName = fieldSchema.Name.LocalName;
        }
    }

    public class NativeField : Field
    {
        public NativeField(XElement fieldSchema)
            : base(fieldSchema)
        {
            string dataType = fieldSchema.Attribute("DataType").Value;
            DataType = Type.GetType(dataType);
        }
    }

    // <FieldName Element="" Field="" [Relationship.Content=""]|[Relationship.Name]|[ReferencePath.Content=""]|[ReferencePath.Name=""] />
    public class ReferenceField : Field
    {
        public string Element { get; private set; }
        public string Field { get; private set; }

        public ReferencePath ReferencePath { get; private set; }

        public ReferenceField(XElement fieldSchema, string elementName, XElement schema)
            : base(fieldSchema)
        {
            Element = fieldSchema.Attribute(Glossary.Element).Value;
            Field = fieldSchema.Attribute(Glossary.Field).Value;

            XElement rElement = schema.GetElementSchema(Element);
            XElement rField = rElement.Element(Field);
            string dataType = rField.Attribute("DataType").Value;
            DataType = Type.GetType(dataType);

            XAttribute attr = fieldSchema.Attribute(string.Format("{0}.{1}", Glossary.Relationship, "Content"));
            if (attr != null)
            {
                ManyToOneRelationship relationship = new ManyToOneRelationship(attr.Value);
                if (relationship.RelatedElementName != Element)
                {
                    XElement relationshipSchema = relationship.Reverse().ToElement();
                    relationshipSchema.SetAttributeValue(Glossary.RelationshipType, "ManyToOne");
                    relationship = new ManyToOneRelationship(relationshipSchema);
                }

                Debug.Assert(relationship.RelatedElementName == Element);

                ReferencePath = new ReferencePath(relationship);
                return;
            }

            attr = fieldSchema.Attribute(string.Format("{0}.{1}", Glossary.Relationship, "Name"));
            if (attr != null)
            {
                XElement relationshipSchema = schema.Elements(Glossary.Relationship).First(p => p.Attribute("Name").Value == attr.Value);
                switch (relationshipSchema.Attribute(Glossary.RelationshipType).Value)
                {
                    case "ManyToOne":
                        if (relationshipSchema.Attribute(Glossary.RelationshipTo).Value == Element)
                        {
                            ManyToOneRelationship relationship = new ManyToOneRelationship(relationshipSchema);
                            ReferencePath = new ReferencePath(relationship);
                            return;
                        }
                        break;
                    case "OneToOne":
                        {
                            OneToOneRelationship relationship = new OneToOneRelationship(relationshipSchema);
                            if (relationshipSchema.Attribute(Glossary.RelationshipTo).Value == Element)
                            {
                                ReferencePath = new ReferencePath(relationship);
                                return;
                            }
                            else
                            {
                                ReferencePath = new ReferencePath(relationship.Reverse() as OneToOneRelationship);
                                return;
                            }
                        }
                    case "OneToMany":
                        if (relationshipSchema.Attribute(Glossary.RelationshipFrom).Value == Element)
                        {
                            OneToManyRelationship relationship = new OneToManyRelationship(relationshipSchema);
                            ReferencePath = new ReferencePath(relationship.Reverse() as ManyToOneRelationship);
                            return;
                        }
                        break;
                }
            }

            attr = fieldSchema.Attribute(string.Format("{0}.{1}", Glossary.ReferencePath, "Content"));
            if (attr != null)
            {
                ReferencePath = new ReferencePath(attr.Value);
                return;
            }

            attr = fieldSchema.Attribute(string.Format("{0}.{1}", Glossary.ReferencePath, "Name"));
            if (attr != null)
            {
                XElement path = schema.Elements(Glossary.ReferencePath).First(p => p.Attribute("Name").Value == attr.Value);
                ReferencePath = new ReferencePath(path);
                return;
            }

            //
            SimpleRelationship toOneRelationship = schema.CreatePrimeToOneRelationship(elementName, Element);
            if (toOneRelationship != null)
            {
                if (toOneRelationship is ManyToOneRelationship)
                {
                    ReferencePath = new ReferencePath(toOneRelationship as ManyToOneRelationship);
                }
                else
                {
                    ReferencePath = new ReferencePath(toOneRelationship as OneToOneRelationship);
                }
                return;
            }

            XElement referencePathSchema = schema.GetPrimeReferencePathSchema(elementName, Element);
            if (referencePathSchema != null)
            {
                ReferencePath = new ReferencePath(referencePathSchema);
                return;
            }

            //
            toOneRelationship = schema.CreateToOneRelationship(elementName, Element);
            if (toOneRelationship != null)
            {
                if (toOneRelationship is ManyToOneRelationship)
                {
                    ReferencePath = new ReferencePath(toOneRelationship as ManyToOneRelationship);
                }
                else
                {
                    ReferencePath = new ReferencePath(toOneRelationship as OneToOneRelationship);
                }
                return;
            }

            referencePathSchema = schema.GetReferencePathSchema(elementName, Element);
            if (referencePathSchema != null)
            {
                ReferencePath = new ReferencePath(referencePathSchema);
                return;
            }
        }


    }
}
