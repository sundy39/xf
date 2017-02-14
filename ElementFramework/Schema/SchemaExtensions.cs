using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Schema
{
    public static class SchemaExtensions
    {
        public static string ExtractSchemaKey(this XElement schema)
        {
            string source;
            string key = ExtractSchemaKey(schema, out source);
            if (source == null) return key;
            return null;
        }

        // source: client id
        public static string ExtractSchemaKey(this XElement schema, out string source)
        {
            XElement key = new XElement(schema);
            key.RemoveNodes();
            if (schema.Attribute("Source") == null)
            {
                source = null;
            }
            else
            {
                source = schema.Attribute("Source").Value;
            }
            return key.ToString();
        }

        public static SimpleRelationship CreatePrimeToOneRelationship(this XElement schema, string from, string to)
        {
            IEnumerable<XElement> relationshipSchemas = schema.Elements(Glossary.Relationship).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null && x.Element(Glossary.Prime) != null).Where(x =>
                    (x.Attribute(Glossary.RelationshipFrom).Value == from && x.Attribute(Glossary.RelationshipTo).Value == to &&
                        (x.Attribute(Glossary.RelationshipType).Value == "ManyToOne" || x.Attribute(Glossary.RelationshipType).Value == "OneToOne")) ||
                    (x.Attribute(Glossary.RelationshipFrom).Value == to && x.Attribute(Glossary.RelationshipTo).Value == from &&
                        (x.Attribute(Glossary.RelationshipType).Value == "OneToMany" || x.Attribute(Glossary.RelationshipType).Value == "OneToOne"))
                );

            int count = relationshipSchemas.Count();
            if (count == 0) return null;
            if (count > 1) throw new SchemaException(string.Format(Messages.Duplicate_PrimeRelationship, from, to), schema);

            XElement relationshipSchema = relationshipSchemas.First();
            Relationship relationship = relationshipSchema.CreateRelationship();
            if (relationship.From == to && relationship.To == from)
            {
                return relationship.Reverse() as SimpleRelationship;
            }
            return relationship as SimpleRelationship;
        }

        public static XElement GetPrimeReferencePathSchema(this XElement schema, string from, string to)
        {
            IEnumerable<XElement> referencePathSchemas = schema.Elements(Glossary.ReferencePath).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null && x.Element(Glossary.Prime) != null &&
                x.Attribute(Glossary.RelationshipFrom).Value == from && x.Attribute(Glossary.RelationshipTo).Value == to);
            int count = referencePathSchemas.Count();
            if (count == 0) return null;
            if (count > 1) throw new SchemaException(string.Format(Messages.Duplicate_PrimeReferencePath, from, to), schema);

            XElement referencePathSchema = referencePathSchemas.First();
            return referencePathSchema;
        }

        public static ManyToManyRelationship CreatePrimeManyToManyRelationship(this XElement schema, string from, string to)
        {
            IEnumerable<XElement> relationshipSchemas = schema.Elements(Glossary.Relationship).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null && x.Attribute(Glossary.RelationshipType).Value == "ManyToMany" &&
                x.Element(Glossary.Prime) != null &&
                (x.Attribute(Glossary.RelationshipFrom).Value == from && x.Attribute(Glossary.RelationshipTo).Value == to ||
                 x.Attribute(Glossary.RelationshipFrom).Value == to && x.Attribute(Glossary.RelationshipTo).Value == from)
                );
            int count = relationshipSchemas.Count();
            if (count == 0) return null;
            if (count > 1) throw new SchemaException(string.Format(Messages.Duplicate_PrimeRelationship, from, to), schema);

            XElement relationshipSchema = relationshipSchemas.First();
            ManyToManyRelationship relationship = new ManyToManyRelationship(relationshipSchema);

            if (relationship.From == to && relationship.To == from)
            {
                return relationship.Reverse() as ManyToManyRelationship;
            }
            return relationship;
        }

        public static ManyToManyRelationship CreateManyToManyRelationship(this XElement schema, string from, string to)
        {
            IEnumerable<XElement> relationshipSchemas = schema.Elements(Glossary.Relationship).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null && x.Attribute(Glossary.RelationshipType).Value == "ManyToMany" &&
                (x.Attribute(Glossary.RelationshipFrom).Value == from && x.Attribute(Glossary.RelationshipTo).Value == to ||
                 x.Attribute(Glossary.RelationshipFrom).Value == to && x.Attribute(Glossary.RelationshipTo).Value == from)
                );

            return CreateRelationship(schema, from, to, relationshipSchemas) as ManyToManyRelationship;
        }

        public static SimpleRelationship CreateToOneRelationship(this XElement schema, string from, string to)
        {
            IEnumerable<XElement> relationshipSchemas = schema.Elements(Glossary.Relationship).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null).Where(x =>
                    (x.Attribute(Glossary.RelationshipFrom).Value == from && x.Attribute(Glossary.RelationshipTo).Value == to &&
                        (x.Attribute(Glossary.RelationshipType).Value == "ManyToOne" || x.Attribute(Glossary.RelationshipType).Value == "OneToOne")) ||
                    (x.Attribute(Glossary.RelationshipFrom).Value == to && x.Attribute(Glossary.RelationshipTo).Value == from &&
                        (x.Attribute(Glossary.RelationshipType).Value == "OneToMany" || x.Attribute(Glossary.RelationshipType).Value == "OneToOne"))
                );

            return CreateRelationship(schema, from, to, relationshipSchemas) as SimpleRelationship;
        }

        public static Relationship CreateRelationship(this XElement schema, string from, string to)
        {
            IEnumerable<XElement> relationshipSchemas = schema.Elements(Glossary.Relationship).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null &&
                    (x.Attribute(Glossary.RelationshipFrom).Value == from && x.Attribute(Glossary.RelationshipTo).Value == to ||
                     x.Attribute(Glossary.RelationshipFrom).Value == to && x.Attribute(Glossary.RelationshipTo).Value == from));

            return CreateRelationship(schema, from, to, relationshipSchemas);
        }

        public static SimpleRelationship CreateSimpleRelationship(this XElement schema, string from, string to)
        {
            IEnumerable<XElement> relationshipSchemas = schema.Elements(Glossary.Relationship).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null && x.Attribute(Glossary.RelationshipType).Value != "ManyToMany" &&
                    (x.Attribute(Glossary.RelationshipFrom).Value == from && x.Attribute(Glossary.RelationshipTo).Value == to ||
                     x.Attribute(Glossary.RelationshipFrom).Value == to && x.Attribute(Glossary.RelationshipTo).Value == from));

            return CreateRelationship(schema, from, to, relationshipSchemas) as SimpleRelationship;
        }

        private static Relationship CreateRelationship(XElement schema, string from, string to, IEnumerable<XElement> relationshipSchemas)
        {
            int count = relationshipSchemas.Count();
            if (count == 0) return null;
            XElement relationshipSchema;
            if (count == 1)
            {
                relationshipSchema = relationshipSchemas.First();
            }
            else
            {
                IEnumerable<XElement> primeSchemas = relationshipSchemas.Where(x => x.Element(Glossary.Prime) != null);
                int primeCount = primeSchemas.Count();
                if (primeCount == 0)
                {
                    throw new SchemaException(string.Format(Messages.PrimeRelationship_Not_Specified,
                    Glossary.Prime, from, to), schema);
                }
                if (primeCount == 1) relationshipSchema = primeSchemas.First();
                throw new SchemaException(string.Format(Messages.Duplicate_PrimeRelationship, from, to), schema);
            }
            Relationship relationship = relationshipSchema.CreateRelationship();
            if (relationship.From == to && relationship.To == from)
            {
                return relationship.Reverse();
            }
            return relationship;
        }

        public static XElement GetReferencePathSchema(this XElement schema, string from, string to)
        {
            IEnumerable<XElement> referencePathSchemas = schema.Elements(Glossary.ReferencePath).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null &&
                x.Attribute(Glossary.RelationshipFrom).Value == from && x.Attribute(Glossary.RelationshipTo).Value == to);
            int count = referencePathSchemas.Count();
            if (count == 0) return null;
            XElement referencePathSchema;
            if (count == 1)
            {
                referencePathSchema = referencePathSchemas.First();
            }
            else
            {
                IEnumerable<XElement> primeSchemas = referencePathSchemas.Where(x => x.Element(Glossary.Prime) != null);
                int primeCount = primeSchemas.Count();
                if (primeCount == 0) throw new SchemaException(string.Format(
                    Messages.PrimeReferencePath_Not_Specified, Glossary.Prime, from, to), schema);

                if (primeCount == 1) referencePathSchema = primeSchemas.First();

                throw new SchemaException(string.Format(Messages.Duplicate_PrimeReferencePath, from, to), schema);
            }
            return referencePathSchema;
        }

        public static bool IsSet(this XElement schema, XElement element)
        {
            return schema.GetElementSchemaBySetName(element.Name.LocalName) != null;
        }

        public static XElement GetElementSchemaBySetName(this XElement schema, string setName)
        {
            return schema.Elements().FirstOrDefault(p => p.Attribute(Glossary.Set) != null && p.Attribute(Glossary.Set).Value == setName);
        }

        public static bool IsElement(this XElement schema, XElement element)
        {
            return schema.GetElementSchema(element.Name.LocalName) != null;
        }

        public static XElement GetElementSchema(this XElement schema, string elementName)
        {
            return schema.Elements(elementName).FirstOrDefault(p => p.Attribute(Glossary.Set) != null);
        }

        //
        public static void Modify(this XElement schema, XElement modifying)
        {
            Modify(schema, modifying, false);
        }

        public static void ModifyWithXAttributes(this XElement schema, XElement modifying)
        {
            if (modifying == null) return;
            if (modifying.Name == schema.Name)
            {
                foreach (XAttribute attr in modifying.Attributes())
                {
                    schema.SetAttributeValue(attr.Name, attr.Value);
                }
            }
            Modify(schema, modifying, true);
        }

        private static void Modify(XElement schema, XElement modifying, bool withXAttributes)
        {
            if (modifying == null) return;

            Remove(schema, modifying);

            var modifyingElements = modifying.Elements().Where(x => x.Attribute(Glossary.RelationshipContent) == null);

            foreach (XElement modifyingElement in modifyingElements)
            {
                XElement elementSchema = schema.Elements(modifyingElement.Name).FirstOrDefault(x => x.Attribute(Glossary.Set) != null);
                if (elementSchema == null)
                {
                    schema.Add(modifyingElement);
                }
                else
                {
                    if (withXAttributes)
                    {
                        foreach (XAttribute attr in modifyingElement.Attributes())
                        {
                            elementSchema.SetAttributeValue(attr.Name, attr.Value);
                        }
                    }

                    //
                    foreach (XElement modifyingField in modifyingElement.Elements())
                    {
                        XElement fieldSchema = elementSchema.Element(modifyingField.Name);

                        //
                        if (withXAttributes)
                        {
                            foreach (XAttribute attr in modifyingField.Attributes())
                            {
                                fieldSchema.SetAttributeValue(attr.Name, attr.Value);
                            }
                        }

                        if (fieldSchema == null)
                        {
                            elementSchema.Add(modifyingField);
                        }
                        else
                        {
                            foreach (XElement attributeElement in modifyingField.Elements())
                            {
                                XElement attributeSchema = fieldSchema.Element(attributeElement.Name);
                                if (attributeSchema != null) attributeSchema.Remove();
                                fieldSchema.Add(attributeElement);
                            }
                        }
                    }
                }
            }

            //
            var relationships = modifying.Elements(Glossary.Relationship).Where(x => x.Attribute(Glossary.RelationshipContent) != null);
            foreach (XElement relationship in relationships)
            {
                XElement relationshipSchema = schema.Elements(Glossary.Relationship).FirstOrDefault(
                    p => p.Attribute(Glossary.RelationshipContent) != null &&
                    p.Attribute(Glossary.RelationshipContent).Value == relationship.Attribute(Glossary.RelationshipContent).Value);
                if (relationshipSchema == null)
                {
                    schema.Add(relationship);
                }
                else
                {
                    string relationshipName = (relationship.Attribute("Name") == null) ? null : relationship.Attribute("Name").Value;
                    string relationshipSchemaName = (relationshipSchema.Attribute("Name") == null) ? null : relationshipSchema.Attribute("Name").Value;
                    if (relationshipName == relationshipSchemaName)
                    {
                        relationshipSchema.Remove();
                    }
                    schema.Add(relationship);
                }

                //
                if (relationship.Element(Glossary.Prime) != null)
                {
                    IEnumerable<XElement> fromToSchemas = schema.Elements(Glossary.Relationship).Where(x => x.Attribute(Glossary.RelationshipContent) != null &&
                        x.Attribute(Glossary.RelationshipType).Value == relationship.Attribute(Glossary.RelationshipType).Value &&
                        x.Attribute(Glossary.RelationshipFrom).Value == relationship.Attribute(Glossary.RelationshipFrom).Value &&
                        x.Attribute(Glossary.RelationshipTo).Value == relationship.Attribute(Glossary.RelationshipTo).Value);

                    string toFromRelationshipType;
                    switch (relationship.Attribute(Glossary.RelationshipType).Value)
                    {
                        case "ManyToOne":
                            toFromRelationshipType = "OneToMany";
                            break;
                        case "OneToMany":
                            toFromRelationshipType = "ManyToOne";
                            break;
                        default:
                            // OneToOne, ManyToMany
                            toFromRelationshipType = relationship.Attribute(Glossary.RelationshipType).Value;
                            break;

                    }

                    IEnumerable<XElement> toFromSchemas = schema.Elements(Glossary.Relationship).Where(x => x.Attribute(Glossary.RelationshipContent) != null &&
                         x.Attribute(Glossary.RelationshipType).Value == toFromRelationshipType &&
                         x.Attribute(Glossary.RelationshipFrom).Value == relationship.Attribute(Glossary.RelationshipTo).Value &&
                         x.Attribute(Glossary.RelationshipTo).Value == relationship.Attribute(Glossary.RelationshipFrom).Value);

                    foreach (XElement fromToSchema in fromToSchemas)
                    {
                        if (fromToSchema.Element(Glossary.Prime) != null)
                            fromToSchema.Element(Glossary.Prime).Remove();
                    }

                    foreach (XElement toFromSchema in toFromSchemas)
                    {
                        if (toFromSchema.Element(Glossary.Prime) != null)
                            toFromSchema.Element(Glossary.Prime).Remove();
                    }

                    relationshipSchema.Add(new XElement(Glossary.Prime));
                }
            }

            //
            var referencePaths = modifying.Elements(Glossary.ReferencePath).Where(x => x.Attribute(Glossary.RelationshipContent) != null);
            foreach (XElement referencePath in referencePaths)
            {
                XElement referencePathSchema = schema.Elements(Glossary.ReferencePath).FirstOrDefault(x => x.Attribute(Glossary.RelationshipContent) != null &&
                  x.Attribute(Glossary.RelationshipContent).Value == referencePath.Attribute(Glossary.RelationshipContent).Value);
                if (referencePathSchema == null)
                {
                    schema.Add(referencePath);
                }
                else
                {
                    string pathName = (referencePath.Attribute("Name") == null) ? null : referencePath.Attribute("Name").Value;
                    string pathSchemaName = (referencePathSchema.Attribute("Name") == null) ? null : referencePathSchema.Attribute("Name").Value;
                    if (pathName == pathSchemaName)
                    {
                        referencePathSchema.Remove();
                    }
                    schema.Add(referencePath);
                }

                //
                if (referencePath.Element(Glossary.Prime) != null)
                {
                    IEnumerable<XElement> fromToSchemas = schema.Elements(Glossary.ReferencePath).Where(x => x.Attribute(Glossary.RelationshipContent) != null &&
                        x.Attribute(Glossary.RelationshipFrom).Value == referencePath.Attribute(Glossary.RelationshipFrom).Value &&
                        x.Attribute(Glossary.RelationshipTo).Value == referencePath.Attribute(Glossary.RelationshipTo).Value);

                    foreach (XElement fromToSchema in fromToSchemas)
                    {
                        if (fromToSchema.Element(Glossary.Prime) != null)
                            fromToSchema.Element(Glossary.Prime).Remove();
                    }

                    referencePath.Add(new XElement(Glossary.Prime));
                }
            }
        }

        //<Remove Element="User" />
        //<Remove Element="User" Field="CreationDate" />
        //<Remove Element="User" Field="CreationDate" Annotation="Required" />

        //<Remove Elements="User,Role" />
        //<Remove Element="User" Fields="Id,CreationDate" />
        //<Remove Element="User" Field="CreationDate" Annotations="Required,DefaultValue" />

        //<Remove Relationship.Content="UserRole(RoleId),Role(Id)" />
        //<Remove Relationship.Content="UserRole(RoleId),Role(Id)" Annotation="Prime" />
        //<Remove Relationship.Name="..." />

        //<Remove ReferencePath.Content="..." />
        //<Remove ReferencePath.Name=".." />
        private static void Remove(XElement schema, XElement modifying)
        {
            var elementRemoves = modifying.Elements(Glossary.Remove).Where(x => x.Attribute(Glossary.Element) != null);
            foreach (XElement remove in elementRemoves)
            {
                string element = remove.Attribute(Glossary.Element).Value;
                XElement elementSchema = schema.Elements(element).FirstOrDefault(x => x.Attribute(Glossary.Set) != null);
                if (elementSchema == null) continue;
                if (remove.Attribute(Glossary.Field) == null && remove.Attribute(Glossary.Fields) == null)
                {
                    elementSchema.Remove();
                }
                else
                {
                    if (remove.Attribute(Glossary.Fields) != null)
                    {
                        string[] fieldNames = remove.Attribute(Glossary.Fields).Value.Split(',');
                        foreach (string fieldName in fieldNames)
                        {
                            if (elementSchema.Element(fieldName.Trim()) == null) continue;
                            elementSchema.Element(fieldName.Trim()).Remove();
                        }
                    }
                    if (remove.Attribute(Glossary.Field) != null)
                    {
                        string fieldName = remove.Attribute(Glossary.Field).Value;
                        if (remove.Attribute(Glossary.Annotation) == null && remove.Attribute(Glossary.Annotations) == null)
                        {
                            if (elementSchema.Element(fieldName) != null) elementSchema.Element(fieldName).Remove();
                        }
                        else
                        {
                            if (remove.Attribute(Glossary.Annotation) != null)
                            {
                                string annotation = remove.Attribute(Glossary.Annotation).Value;
                                if (elementSchema.Element(fieldName).Element(annotation) != null) elementSchema.Element(fieldName).Element(annotation).Remove();
                            }

                            if (remove.Attribute(Glossary.Annotations) != null)
                            {
                                string[] annotations = remove.Attribute(Glossary.Annotations).Value.Split(',');
                                foreach (string annotation in annotations)
                                {
                                    if (elementSchema.Element(fieldName).Element(annotation) == null) continue;
                                    elementSchema.Element(fieldName).Element(annotation).Remove();
                                }
                            }
                        }
                    }
                }
            }

            //
            var elementsRemoves = modifying.Elements(Glossary.Remove).Where(x => x.Attribute(Glossary.Elements) != null);
            foreach (XElement remove in elementsRemoves)
            {
                string elements = remove.Attribute(Glossary.Elements).Value;
                string[] elementNames = elements.Split(',');
                foreach (string elementName in elementNames)
                {
                    XElement elementSchema = schema.Elements(elementName.Trim()).FirstOrDefault(x => x.Attribute(Glossary.Set) != null);
                    if (elementSchema == null) continue;
                    elementSchema.Remove();
                }
            }

            //
            var relationshipRemoves = modifying.Elements(Glossary.Remove).Where(x =>
                x.Attribute(string.Format("{0}.{1}", Glossary.Relationship, Glossary.RelationshipContent)) != null ||
                x.Attribute(string.Format("{0}.Name", Glossary.Relationship)) != null);
            foreach (XElement remove in relationshipRemoves)
            {
                string content = remove.Attribute(string.Format("{0}.{1}", Glossary.Relationship, Glossary.RelationshipContent)).Value;
                var relationshipSchema = schema.Elements(Glossary.Relationship).FirstOrDefault(x => x.Attribute(Glossary.RelationshipContent) != null && x.Attribute(Glossary.RelationshipContent).Value == content);
                if (relationshipSchema == null) continue;
                if (remove.Attribute(Glossary.Annotation) == null && remove.Attribute(Glossary.Annotations) == null)
                {
                    relationshipSchema.Remove();
                }
                else
                {
                    if (remove.Attribute(Glossary.Annotation) != null)
                    {
                        string annotation = remove.Attribute(Glossary.Annotation).Value;
                        if (relationshipSchema.Element(annotation) != null) relationshipSchema.Element(annotation).Remove();
                    }
                }
            }

            //
            var referencePathRemoves = modifying.Elements(Glossary.Remove).Where(x =>
                x.Attribute(string.Format("{0}.{1}", Glossary.ReferencePath, Glossary.RelationshipContent)) != null ||
                x.Attribute(string.Format("{0}.Name", Glossary.ReferencePath)) != null);
            foreach (XElement remove in referencePathRemoves)
            {
                XAttribute contentAttr = remove.Attribute(string.Format("{0}.{1}", Glossary.Relationship, Glossary.RelationshipContent));
                if (contentAttr == null)
                {
                    string name = remove.Attribute(string.Format("{0}.Name", Glossary.Relationship)).Value;
                    var referencePathSchema = schema.Elements(Glossary.ReferencePath).FirstOrDefault(x =>
                        x.Attribute("Name") != null &&
                        x.Attribute("Name").Value == name);
                    if (referencePathSchema != null)
                    {
                        referencePathSchema.Remove();
                    }
                }
                else
                {
                    var referencePathSchema = schema.Elements(Glossary.ReferencePath).FirstOrDefault(x =>
                        x.Attribute(Glossary.RelationshipContent) != null &&
                        x.Attribute(Glossary.RelationshipContent).Value == contentAttr.Value);
                    if (referencePathSchema != null)
                    {
                        referencePathSchema.Remove();
                    }
                }
            }
        }

        public static IEnumerable<XElement> DeduceOutManyToManyRelationships(this XElement schema)
        {
            Dictionary<string, XElement> relationships = new Dictionary<string, XElement>();

            //
            var manyToOnes = schema.Elements(Glossary.Relationship).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null &&
                x.Attribute(Glossary.RelationshipType).Value == "ManyToOne");
            foreach (XElement relationship in manyToOnes)
            {
                string key = relationship.Attribute(Glossary.RelationshipContent).Value;
                if (!relationships.ContainsKey(key))
                {
                    relationships.Add(key, relationship);
                }
            }

            //
            var oneToManys = schema.Elements(Glossary.Relationship).Where(x =>
                 x.Attribute(Glossary.RelationshipContent) != null &&
                 x.Attribute(Glossary.RelationshipType).Value == "OneToMany");
            foreach (XElement oneToMany in oneToManys)
            {
                XElement relationship = new OneToManyRelationship(oneToMany).Reverse().ToElement();
                string key = relationship.Attribute(Glossary.RelationshipContent).Value;
                if (!relationships.ContainsKey(key))
                {
                    relationships.Add(key, relationship);
                }
            }

            //
            var oneToOnes = schema.Elements(Glossary.Relationship).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null &&
                x.Attribute(Glossary.RelationshipType).Value == "OneToOne");

            foreach (XElement oneToOne in oneToOnes)
            {
                XElement relationship = oneToOne;
                string key = relationship.Attribute(Glossary.RelationshipContent).Value;
                if (!relationships.ContainsKey(key))
                {
                    relationships.Add(key, relationship);
                }

                relationship = new OneToOneRelationship(oneToOne).Reverse().ToElement();
                key = relationship.Attribute(Glossary.RelationshipContent).Value;
                if (!relationships.ContainsKey(key))
                {
                    relationships.Add(key, relationship);
                }
            }

            //
            List<XElement> result = new List<XElement>();
            var manyToManys = DeduceOutManyToManyRelationships(relationships.Values);
            foreach (XElement manyToMany in manyToManys)
            {
                string content = manyToMany.Attribute(Glossary.RelationshipContent).Value;

                if (schema.Elements(Glossary.Relationship).Any(x =>
                    x.Attribute(Glossary.RelationshipType).Value == "ManyToMany" &&
                    x.Attribute(Glossary.RelationshipContent) != null &&
                    x.Attribute(Glossary.RelationshipContent).Value == content))
                {
                    continue;
                }

                //
                content = new ManyToManyRelationship(manyToMany).Reverse().Content;
                if (schema.Elements(Glossary.Relationship).Any(x =>
                x.Attribute(Glossary.RelationshipType).Value == "ManyToMany" &&
                x.Attribute(Glossary.RelationshipContent) != null &&
                x.Attribute(Glossary.RelationshipContent).Value == content))
                {
                    continue;
                }

                //
                result.Add(manyToMany);
            }
            return result;
        }

        // relationships: ManyToOne
        private static IEnumerable<XElement> DeduceOutManyToManyRelationships(IEnumerable<XElement> relationships)
        {
            Dictionary<string, List<XElement>> dict = new Dictionary<string, List<XElement>>();
            foreach (XElement relationship in relationships)
            {
                string from = relationship.Attribute(Glossary.RelationshipFrom).Value;
                if (dict.ContainsKey(from))
                {
                    dict[from].Add(relationship);
                }
                else
                {
                    dict.Add(from, new List<XElement>() { relationship });
                }
            }

            List<XElement> result = new List<XElement>();
            foreach (var pair in dict)
            {
                if (pair.Value.Count == 2)
                {
                    OneToManyRelationship oneToManyRelationship = new ManyToOneRelationship(pair.Value[0]).Reverse() as OneToManyRelationship;
                    ManyToOneRelationship manyToOneRelationship = new ManyToOneRelationship(pair.Value[1]);

                    ManyToManyRelationship manyToManyRelationship = new ManyToManyRelationship(oneToManyRelationship, manyToOneRelationship);

                    if (relationships.Any(x => x.Attribute(Glossary.RelationshipContent).Value == manyToManyRelationship.Content)) continue;
                    if (relationships.Any(x => x.Attribute(Glossary.RelationshipContent).Value == manyToManyRelationship.Reverse().Content)) continue;

                    result.Add(manyToManyRelationship.ToElement());
                }
            }

            return result;
        }

        public static XElement InferReferencePath(this XElement schema, string from, string to)
        {
            ReferencePathGenerator referencePathGenerator = new ReferencePathGenerator(schema);
            IEnumerable<XElement> result = referencePathGenerator.Generate(from, to);
            if (result.Count() == 1) return result.First();
            return null;
        }

    }

    public static class RelationshipSchemaExtensions
    {
        public static Relationship CreateRelationship(this XElement relationshipSchema)
        {
            Relationship relationship;
            string relationshipType = relationshipSchema.Attribute(Glossary.RelationshipType).Value;
            switch (relationshipType)
            {
                case "ManyToOne":
                    relationship = new ManyToOneRelationship(relationshipSchema);
                    break;
                case "OneToMany":
                    relationship = new OneToManyRelationship(relationshipSchema);
                    break;
                case "OneToOne":
                    relationship = new OneToOneRelationship(relationshipSchema);
                    break;
                case "ManyToMany":
                    relationship = new ManyToManyRelationship(relationshipSchema);
                    break;
                default:
                    throw new NotSupportedException(relationshipType);
            }
            return relationship;
        }
    }

    public static class ElementSchemaExtensions
    {
        public static XElement GetEmptyElement(this XElement elementSchema)
        {
            XElement fields = new XElement(elementSchema.Name);
            foreach (XElement fieldSchema in elementSchema.Elements())
            {
                if (fieldSchema.Attribute(Glossary.Element) != null && fieldSchema.Attribute(Glossary.Field) == null) continue;
                fields.Add(new XElement(fieldSchema.Name));
            }
            return fields;
        }

        public static XElement GetKeySchema(this XElement elementSchema)
        {
            XElement keySchema = new XElement(elementSchema.Name);
            IEnumerable<XElement> keyFields = elementSchema.Elements().Where(x => x.Element("Key") != null);
            keySchema.Add(keyFields);
            return keySchema.HasElements ? keySchema : null;
        }

        public static XElement GetTimestampSchema(this XElement elementSchema)
        {
            return elementSchema.Elements().FirstOrDefault(x => x.Element("Timestamp") != null);
        }

        public static XElement GetConcurrencyCheckSchema(this XElement elementSchema)
        {
            XElement schema = new XElement(elementSchema.Name);
            IEnumerable<XElement> fields = elementSchema.Elements().Where(x => x.Element("Timestamp") == null && x.Element("ConcurrencyCheck") != null);
            schema.Add(fields);
            return schema.HasElements ? schema : null;
        }
    }

    public static class FieldSchemaExtensions
    {
        public static XElement ExtractKey(this XElement keySchema, XElement element)
        {
            XElement key = new XElement(keySchema.Name);
            foreach (XElement fieldSchema in keySchema.Elements())
            {
                if (element.Element(fieldSchema.Name) == null) return null;
                key.SetElementValue(fieldSchema.Name, element.Element(fieldSchema.Name).Value);
            }
            return key.HasElements ? key : null;
        }

        public static XElement ExtractTimestamp(this XElement timestampFieldSchema, XElement element)
        {
            return element.Element(timestampFieldSchema.Name);
        }

        public static XElement ExtractConcurrencyChecks(this XElement concurrencyCheckSchema, XElement element)
        {
            XElement elmt = new XElement(element.Name);
            foreach (XElement fieldSchema in concurrencyCheckSchema.Elements())
            {
                elmt.SetElementValue(fieldSchema.Name, element.Element(fieldSchema.Name).Value);
            }
            return elmt.HasElements ? elmt : null;
        }

        public static List<ValidationAttribute> CreateValidationAttributes(this XElement fieldSchema)
        {
            List<ValidationAttribute> validationAttributes = new List<ValidationAttribute>();
            foreach (XElement annotation in fieldSchema.Elements())
            {
                ValidationAttribute validationAttribute = null;
                switch (annotation.Name.LocalName)
                {
                    case "CustomValidation":
                        validationAttribute = annotation.CreateCustomValidationAttribute();
                        break;
                    case "DataType":
                        validationAttribute = annotation.CreateDataTypeAttribute();
                        break;
                    case "Range":
                        validationAttribute = annotation.CreateRangeAttribute();
                        break;
                    case "RegularExpression":
                        validationAttribute = annotation.CreateRegularExpressionAttribute();
                        break;
                    case "Required":
                        validationAttribute = annotation.CreateRequiredAttribute();
                        break;
                    case "StringLength":
                        validationAttribute = annotation.CreateStringLengthAttribute();
                        break;

                    // .NET Framework 4.5
                    case "MaxLength":
                        validationAttribute = annotation.CreateMaxLengthAttribute();
                        break;
                    case "MinLength":
                        validationAttribute = annotation.CreateMinLengthAttribute();
                        break;
                    case "CreditCard":
                        validationAttribute = annotation.CreateCreditCardAttribute();
                        break;
                    case "EmailAddress":
                        validationAttribute = annotation.CreateEmailAddressAttribute();
                        break;
                    case "Phone":
                        validationAttribute = annotation.CreatePhoneAttribute();
                        break;
                    case "Url":
                        validationAttribute = annotation.CreateUrlAttribute();
                        break;
                }
                if (validationAttribute != null)
                {
                    validationAttributes.Add(validationAttribute);
                }
            }
            return validationAttributes;
        }

        public static object GetObject(this XElement fieldSchema, string value)
        {
            Type type = Type.GetType(fieldSchema.Attribute("DataType").Value);
            if (value == string.Empty)
            {
                if (type == typeof(string))
                {
                    if (fieldSchema.Attribute("AllowDBNull").Value.ToLower() == bool.TrueString.ToLower())
                    {
                        return null;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return null;
                }
            }
            else if (type == typeof(DateTime))
            {
                return DateTime.Parse(value);
            }
            else if (type == typeof(byte[]))
            {
                string hex = value.StartsWith("0x") ? value.Substring("0x".Length) : value;
                byte[] bytes = new byte[hex.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
                return bytes;
            }
            else if (type == typeof(Guid))
            {
                return Guid.Parse(value);
            }
            else
            {
                return Convert.ChangeType(value, type);
            }
        }

        public static string GetValue(this XElement fieldSchema, object obj)
        {
            Type type = Type.GetType(fieldSchema.Attribute("DataType").Value);
            if (obj == null) return string.Empty;
            else if (type == typeof(DateTime))
            {
                return ((DateTime)(obj)).ToCanonicalString();
            }
            else if (type == typeof(byte[]))
            {
                byte[] bytes = (byte[])obj;
                return "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
            }
            else
            {
                return obj.ToString();
            }
        }

        public static string GetDisplayName(this XElement fieldSchema)
        {
            string displayName = fieldSchema.Name.LocalName;
            if (fieldSchema.Element("DisplayName") != null)
            {
                displayName = fieldSchema.Element("DisplayName").CreateDisplayNameAttribute().DisplayName;
            }
            if (fieldSchema.Element("Display") != null)
            {
                displayName = fieldSchema.Element("Display").CreateDisplayAttribute().GetName();
            }
            return displayName;
        }
    }

}
