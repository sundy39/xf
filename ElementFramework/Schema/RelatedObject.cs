using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Resources;

namespace XData.Data.Schema
{
    //<Role Set="Rols" ...>
    //  <Id DataType="" .../>
    //  <Name DataType="" ... />
    //  <Users Element="User" [ElementAlias="User"]
    //                        [Relationship.Name="..."(ToMany)]|[ReferencePath.Content="...(Reversed)"]|[ReferencePath.Name="...(Reversed)"]|[RelationshipPath.Content="..."] 
    //                        [Select="Id,UserName"] [OrderBy="Id ASC,UserName DESC"] 
    //                        [Filter="Age ge 12 and Name eq 'John' and LastLogin lt datetime'2000-1-1 12:03'"]>
    //    <Person Element="Person" [Relationship.Name="..."(ToOne)]|[ReferencePath.Content="..."]|[ReferencePath.Name="..."]
    //                             [Select=""] />
    //  </Users>
    //</Role> 
    public abstract class RelatedObject
    {
        public string Element { get; protected set; }
        public string ElementAlias { get; protected set; }
        public RelationshipPath RelationshipPath { get; protected set; }
        public XElement Select { get; protected set; }
        public XElement Filter { get; protected set; }

        public IEnumerable<string> ElementAsParentFields { get; protected set; }

        public string ElementAsChildRelatedElement { get; protected set; }
        public IEnumerable<string> ElementAsChildRelatedFields { get; protected set; }

        protected RelationshipPath FullRelationshipPath;
        public RelationshipPath ReversedFullRelationshipPath { get; protected set; }

        protected List<RelatedObject> ChildList = new List<RelatedObject>();
        public IEnumerable<RelatedObject> Children { get { return ChildList; } }

        protected RelatedObject(XElement fieldSchema, XElement schema)
        {
            Element = fieldSchema.Attribute("Element").Value;
            Select = new XElement("Select");
            if (fieldSchema.Attribute("Select") == null)
            {
                Select.Add(schema.GetElementSchema(Element).GetEmptyElement().Elements());
            }
            else
            {
                string[] ss = fieldSchema.Attribute("Select").Value.Split(',');
                foreach (string s in ss)
                {
                    Select.Add(new XElement(s));
                }
            }

            XAttribute attr = fieldSchema.Attribute("Filter");
            if (attr == null)
            {
                Filter = null;
            }
            else
            {
                XElement filter = new XElement("Filter");
                filter.FillFilter(attr.Value, Element, schema);
                Filter = filter;
            }
        }

        protected void Initialize(XElement fieldSchema, XElement schema)
        {
            ElementAsChildRelatedElement = RelationshipPath.Relationships[0].ElementName;
            ElementAsChildRelatedFields = new List<string>(RelationshipPath.Relationships[0].FieldNames);

            FullRelationshipPath = RelationshipPath;
            ReversedFullRelationshipPath = FullRelationshipPath.Reverse();

            InitializeChildren(this, fieldSchema, schema);
        }

        private static void InitializeChildren(RelatedObject relatedObject, XElement fieldSchema, XElement schema)
        {
            List<string> asParentFields = new List<string>();
            foreach (XElement childSchema in fieldSchema.Elements())
            {
                RelatedObject child = Create(childSchema, relatedObject.Element, schema);
                asParentFields.AddRange(child.RelationshipPath.Relationships[0].FieldNames);
                List<SimpleRelationship> list = new List<SimpleRelationship>(relatedObject.FullRelationshipPath.Relationships);
                list.AddRange(child.RelationshipPath.Relationships);
                child.FullRelationshipPath = new RelationshipPath(list.ToArray());
                child.ReversedFullRelationshipPath = child.FullRelationshipPath.Reverse();
                relatedObject.ChildList.Add(child);
            }
            relatedObject.ElementAsParentFields = asParentFields;
        }

        public static RelatedObject Create(XElement fieldSchema, string elementName, XElement schema)
        {
            string Relationship_Name = string.Format("{0}.{1}", Glossary.Relationship, "Name");
            string ReferencePath_Name = string.Format("{0}.{1}", Glossary.ReferencePath, "Name");
            string ReferencePath_Content = string.Format("{0}.{1}", Glossary.ReferencePath, "Content");
            string RelationshipPath_Content = string.Format("{0}.{1}", Glossary.RelationshipPath, "Content");

            string elementAttrValue = fieldSchema.Attribute("Element").Value;
            XElement relationshipSchema = null;
            XElement referencePathSchema = null;
            ReferencePath referencePath = null;
            RelationshipPath relationshipPath = null;

            // Relationship
            XAttribute attr = fieldSchema.Attribute(Relationship_Name);
            if (attr != null)
            {
                relationshipSchema = schema.Elements(Glossary.Relationship).First(p => p.Attribute("Name").Value == attr.Value);
                Relationship relationship = relationshipSchema.CreateRelationship();
                if (relationship.From == elementAttrValue)
                {
                    relationship = relationship.Reverse();
                }
                if (relationship is ManyToOneRelationship)
                {
                    referencePath = new ReferencePath(relationship as ManyToOneRelationship);
                    return new ReferenceElement(fieldSchema, schema, referencePath);
                }
                else if (relationship is OneToOneRelationship)
                {
                    referencePath = new ReferencePath(relationship as OneToOneRelationship);
                    return new ReferenceElement(fieldSchema, schema, referencePath);
                }
                else 
                {
                    // OneToManyRelationship or ManyToManyRelationship

                    relationshipPath = new RelationshipPath(relationship);
                    return new RelatedSet(fieldSchema, schema, relationshipPath);
                }
            }

            // ReferencePath
            referencePath = null;
            attr = fieldSchema.Attribute(ReferencePath_Content);
            if (attr != null)
            {
                string referencePathContent = attr.Value;
                referencePath = new ReferencePath(referencePathContent);
            }
            if (referencePath == null)
            {
                attr = fieldSchema.Attribute(ReferencePath_Name);
                if (attr != null)
                {
                    referencePathSchema = schema.Elements(Glossary.ReferencePath).First(p => p.Attribute("Name").Value == attr.Value);
                    referencePath = new ReferencePath(referencePathSchema);
                }
            }
            if (referencePath != null)
            {
                if (referencePath.To == elementAttrValue)
                {
                    return new ReferenceElement(fieldSchema, schema, referencePath);
                }
                else
                {
                    relationshipPath = referencePath.Reverse();
                    return new RelatedSet(fieldSchema, schema, relationshipPath);
                }
            }

            // RelationshipPath
            attr = fieldSchema.Attribute(RelationshipPath_Content);
            if (attr != null)
            {
                relationshipPath = new RelationshipPath(attr.Value);
                return new RelatedSet(fieldSchema, schema, relationshipPath);
            }

            // Prime
            SimpleRelationship toOneRelationship = schema.CreatePrimeToOneRelationship(elementName, elementAttrValue);
            if (toOneRelationship != null)
            {
                if (toOneRelationship is ManyToOneRelationship)
                {
                    referencePath = new ReferencePath(toOneRelationship as ManyToOneRelationship);
                    return new ReferenceElement(fieldSchema, schema, referencePath);
                }
                else 
                {
                    // OneToOneRelationship

                    referencePath = new ReferencePath(toOneRelationship as OneToOneRelationship);
                    return new ReferenceElement(fieldSchema, schema, referencePath);
                }
            }

            // ToMany
            toOneRelationship = schema.CreatePrimeToOneRelationship(elementAttrValue, elementName);
            if (toOneRelationship != null)
            {
                Debug.Assert(toOneRelationship is ManyToOneRelationship);

                relationshipPath = new RelationshipPath(toOneRelationship.Reverse());
                return new RelatedSet(fieldSchema, schema, relationshipPath);
            }

            // ManyToMany
            ManyToManyRelationship manyToManyRelationship = schema.CreatePrimeManyToManyRelationship(elementName, elementAttrValue);
            if (manyToManyRelationship != null)
            {
                relationshipPath = new RelationshipPath(manyToManyRelationship);
                return new RelatedSet(fieldSchema, schema, relationshipPath);
            }

            // 
            referencePathSchema = schema.GetPrimeReferencePathSchema(elementName, elementAttrValue);
            if (referencePathSchema != null)
            {
                referencePath = new ReferencePath(referencePathSchema);
                return new ReferenceElement(fieldSchema, schema, referencePath);
            }

            // 
            toOneRelationship = schema.CreateToOneRelationship(elementName, elementAttrValue);
            if (toOneRelationship != null)
            {
                if (toOneRelationship is ManyToOneRelationship)
                {
                    referencePath = new ReferencePath(toOneRelationship as ManyToOneRelationship);
                    return new ReferenceElement(fieldSchema, schema, referencePath);
                }
                else
                {
                    // OneToOneRelationship

                    referencePath = new ReferencePath(toOneRelationship as OneToOneRelationship);
                    return new ReferenceElement(fieldSchema, schema, referencePath);
                }
            }

            //
            toOneRelationship = schema.CreateToOneRelationship(elementAttrValue, elementName);
            if (toOneRelationship != null)
            {
                Debug.Assert(toOneRelationship is ManyToOneRelationship);

                relationshipPath = new RelationshipPath(toOneRelationship.Reverse());
                return new RelatedSet(fieldSchema, schema, relationshipPath);
            }

            //
            manyToManyRelationship = schema.CreateManyToManyRelationship(elementName, elementAttrValue);
            if (manyToManyRelationship != null)
            {
                relationshipPath = new RelationshipPath(manyToManyRelationship);
                return new RelatedSet(fieldSchema, schema, relationshipPath);
            }

            // 
            referencePathSchema = schema.GetReferencePathSchema(elementName, elementAttrValue);
            if (referencePathSchema != null)
            {
                referencePath = new ReferencePath(referencePathSchema);
                return new ReferenceElement(fieldSchema, schema, referencePath);
            }

            return null;
        }

    }

    public class ReferenceElement : RelatedObject
    {
        internal protected ReferenceElement(XElement fieldSchema, XElement schema, ReferencePath referencePath)
            : base(fieldSchema, schema)
        {
            ElementAlias = fieldSchema.Name.LocalName;
            RelationshipPath = referencePath;

            Initialize(fieldSchema, schema);
        }
    }

    public class RelatedSet : RelatedObject
    {
        public string SetName { get; protected set; }

        public XElement[] OrderBys { get; protected set; }

        protected static XElement[] GetOrderBys(string orderByValue)
        {
            List<XElement> orderByList = new List<XElement>();
            string[] ss = orderByValue.Split(',');
            string first = ss[0];
            string[] firstParts = first.Split((char)32);
            XElement orderBy;
            if (firstParts.Length == 2 && firstParts[1].Trim().ToLower() == "desc")
            {
                orderBy = new XElement("OrderByDescending");
            }
            else
            {
                orderBy = new XElement("OrderBy");
            }
            orderBy.Add(new XElement(firstParts[0].Trim()));
            orderByList.Add(orderBy);
            for (int i = 1; i < ss.Length; i++)
            {
                string[] parts = ss[i].Split((char)32);
                XElement thenBy;
                if (parts.Length == 2 && parts[1].Trim().ToLower() == "desc")
                {
                    thenBy = new XElement("ThenByDescending");
                }
                else
                {
                    thenBy = new XElement("ThenBy");
                }
                orderBy.Add(new XElement(parts[0].Trim()));
                orderByList.Add(thenBy);
            }
            return orderByList.ToArray();
        }

        internal protected RelatedSet(XElement fieldSchema, XElement schema, RelationshipPath relationshipPath)
            : base(fieldSchema, schema)
        {
            SetName = fieldSchema.Name.LocalName;
            ElementAlias = (fieldSchema.Attribute("ElementAlias") == null) ?
                Element : fieldSchema.Attribute("ElementAlias").Value;
            if (fieldSchema.Attribute("OrderBy") != null)
            {
                OrderBys = GetOrderBys(fieldSchema.Attribute("OrderBy").Value);
            }
            RelationshipPath = relationshipPath;

            Initialize(fieldSchema, schema);
        }
    }


}
