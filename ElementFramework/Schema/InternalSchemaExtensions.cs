using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema.Validation;

namespace XData.Data.Schema
{
    internal static class InternalSchemaExtensions
    {
        internal static XElement CreatePublishSchema(this XElement schema)
        {
            XElement publishSchema = new XElement(schema);
            publishSchema.Attribute(Glossary.DatabaseKey).Remove();
            publishSchema.Attribute(Glossary.DatabaseVersion).Remove();
            publishSchema.Attribute(Glossary.NameMapVersion).Remove();
            publishSchema.Attribute(Glossary.ConfigVersion).Remove();
            foreach (XElement element in publishSchema.Elements().Where(p => p.Attribute(Glossary.Set) != null))
            {
                element.Attribute(Glossary.Table).Remove();
                element.Attribute(Glossary.TableType).Remove();
                XAttribute elementAttr = element.Attribute("PrimaryKey");
                if (elementAttr != null) elementAttr.Remove();
                foreach (XElement field in element.Elements())
                {
                    XAttribute fieldAttr = field.Attribute("AutoIncrement");
                    if (fieldAttr != null) fieldAttr.Remove();
                    fieldAttr = field.Attribute("SqlDbType");
                    if (fieldAttr != null) fieldAttr.Remove();
                    fieldAttr = field.Attribute("AllowDBNull");
                    if (fieldAttr != null) fieldAttr.Remove();
                    fieldAttr = field.Attribute("Unique");
                    if (fieldAttr != null) fieldAttr.Remove();
                    fieldAttr = field.Attribute("DefaultValue");
                    if (fieldAttr != null) fieldAttr.Remove();
                }
            }
            return publishSchema;
        }

        internal static IEnumerable<ValidationResult> GetValidationErrors(this XElement schema)
        {
            List<ValidationResult> errors = new List<ValidationResult>();
            var elementSchemas = schema.Elements().Where(p => p.Attribute(Glossary.Set) != null);
            var relationshipSchemas = schema.Elements(Glossary.Relationship).Where(p => p.Attribute(Glossary.RelationshipContent) != null);

            //
            foreach (XElement element in elementSchemas.ToList())
            {
                string elementName = element.Name.LocalName;
                if (elementSchemas.Count(p => p.Name.LocalName == elementName) > 1)
                {
                    errors.Add(new ValidationResult(Messages.Validation_Schema_Duplicate_Elements, new string[] { elementName }));
                }

                string setName = element.Attribute(Glossary.Set).Value;
                if (elementSchemas.Any(p => p.Name.LocalName == setName))
                {
                    errors.Add(new ValidationResult(Messages.Validation_Schema_Confuse_ElementName_SetName, new string[] { setName }));
                }
                if (elementSchemas.Count(p => p.Attribute(Glossary.Set).Value == setName) > 1)
                {
                    errors.Add(new ValidationResult(Messages.Validation_Schema_Duplicate_Sets, new string[] { setName }));
                }
            }

            //          
            foreach (XElement relationshipSchema in relationshipSchemas)
            {
                string relationshipType = relationshipSchema.Attribute(Glossary.RelationshipType).Value;
                if (relationshipType == "ManyToMany")
                {
                    try
                    {
                        ManyToManyRelationship relationship = new ManyToManyRelationship(relationshipSchema);
                        ValidateSimpleRelationship(relationship.ManyToOneRelationship, elementSchemas, errors);
                        ValidateSimpleRelationship(relationship.OneToManyRelationship, elementSchemas, errors);
                    }
                    catch (SchemaValidationException e)
                    {
                        errors.AddRange(e.Errors);
                    }
                }
                else
                {
                    SimpleRelationship relationship = null;
                    try
                    {
                        switch (relationshipSchema.Attribute(Glossary.RelationshipType).Value)
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
                            default:
                                errors.Add(new ValidationResult(Messages.Validation_Schema_Relationship_Invalid_Type, new string[] { relationshipSchema.ToString() }));
                                break;
                        }
                        if (relationship != null)
                        {
                            ValidateSimpleRelationship(relationship, elementSchemas, errors);
                        }
                    }
                    catch (SchemaValidationException e)
                    {
                        errors.AddRange(e.Errors);
                    }
                }
            }

            //
            var referencePathSchemas = schema.Elements(Glossary.ReferencePath).Where(p => p.Attribute(Glossary.RelationshipContent) != null);
            foreach (XElement referencePathSchema in referencePathSchemas)
            {
                try
                {
                    ReferencePath referencePath = new ReferencePath(referencePathSchema);
                    for (int i = 0; i < referencePath.ManyToOneRelationships.Length; i++)
                    {
                        List<ValidationResult> errorList = new List<ValidationResult>();
                        ValidateSimpleRelationship(referencePath.ManyToOneRelationships[i], elementSchemas, errorList);
                        errors.AddRange(errorList);
                    }
                }
                catch (SchemaValidationException e)
                {
                    errors.AddRange(e.Errors);
                }
            }

            //
            return errors;
        }

        internal static bool IsValid(this SimpleRelationship simpleRelationship, XElement schema)
        {
            var elementSchemas = schema.Elements().Where(p => p.Attribute(Glossary.Set) != null);
            var errors = new List<ValidationResult>();
            ValidateSimpleRelationship(simpleRelationship, elementSchemas, errors);
            return errors.Count == 0;
        }

        private static void ValidateSimpleRelationship(SimpleRelationship relationship, IEnumerable<XElement> elementSchemas, ICollection<ValidationResult> errors)
        {
            XElement elementSchema = elementSchemas.FirstOrDefault(p => p.Name.LocalName == relationship.ElementName);
            if (elementSchema == null)
            {
                errors.Add(new ValidationResult(string.Format(Messages.Validation_Schema_Not_Exists_Element, relationship.ElementName), new string[] { relationship.ToElement().ToString() }));
            }
            else
            {
                foreach (string fieldName in relationship.FieldNames)
                {
                    XElement fieldSchema = elementSchema.Element(fieldName);
                    if (fieldSchema == null)
                    {
                        errors.Add(new ValidationResult(
                            string.Format(Messages.Validation_Schema_Not_Exists_Field, fieldName, relationship.ElementName),
                            new string[] { relationship.ToString() }));
                    }
                    else
                    {
                        if (fieldSchema.Attribute(Glossary.Element) != null)
                        {
                            errors.Add(new ValidationResult(Messages.Validation_Schema_Relationship_Navigation_Field_Not_Allowed,
                                new string[] { relationship.ToString() }));
                        }
                    }
                }
            }

            elementSchema = elementSchemas.FirstOrDefault(p => p.Name.LocalName == relationship.RelatedElementName);
            if (elementSchema == null)
            {
                errors.Add(new ValidationResult(string.Format(Messages.Validation_Schema_Not_Exists_Element, relationship.RelatedElementName),
                    new string[] { relationship.ToString() }));
            }
            else
            {
                foreach (string fieldName in relationship.RelatedFieldNames)
                {
                    XElement fieldSchema = elementSchema.Element(fieldName);
                    if (fieldSchema == null)
                    {
                        errors.Add(new ValidationResult(
                            string.Format(Messages.Validation_Schema_Not_Exists_Field, fieldName, relationship.RelatedElementName),
                            new string[] { relationship.ToElement().ToString() }));
                    }
                    else
                    {
                        if (fieldSchema.Attribute(Glossary.Element) != null)
                        {
                            errors.Add(new ValidationResult(Messages.Validation_Schema_Relationship_Navigation_Field_Not_Allowed,
                                new string[] { relationship.ToString() }));
                        }
                    }
                }
            }
        }

        internal static XElement GetWarnings(this XElement schema)
        {
            XElement warnings = new XElement("Warnings");

            var relationships = schema.Elements(Glossary.Relationship).Where(x => x.Attribute(Glossary.RelationshipContent) != null&&
                x.Attribute(Glossary.RelationshipType).Value != "ManyToMany");
            AddToWarnings(relationships, warnings);

            relationships = schema.Elements(Glossary.Relationship).Where(x => x.Attribute(Glossary.RelationshipContent) != null &&
                x.Attribute(Glossary.RelationshipType).Value == "ManyToMany");
            AddToWarnings(relationships, warnings);

            //
            Dictionary<string, List<XElement>> referencePathDict = new Dictionary<string, List<XElement>>();
            var referencePaths = schema.Elements(Glossary.ReferencePath).Where(x =>
                x.Attribute(Glossary.RelationshipContent) != null &&
                x.Attribute("Name") == null);
            foreach (XElement referencePath in referencePaths)
            {
                string fromTo = string.Format("{0},{1}", referencePath.Attribute(Glossary.RelationshipFrom).Value,
                    referencePath.Attribute(Glossary.RelationshipTo).Value);
                if (referencePathDict.ContainsKey(fromTo))
                {
                    referencePathDict[fromTo].Add(referencePath);
                }
                else
                {
                    referencePathDict.Add(fromTo, new List<XElement>() { referencePath });
                }
            }
            foreach (KeyValuePair<string, List<XElement>> pair in referencePathDict)
            {
                if (pair.Value.Count > 1)
                {
                    string message = Messages.Duplicate_ReferencePath_Name;
                    XElement warning = new XElement("Warning", message);
                    StringBuilder sb = new StringBuilder();
                    foreach (XElement referencePath in pair.Value)
                    {
                        sb.AppendLine(referencePath.ToString());
                    }
                    warning.Add(new XComment(sb.ToString()));
                    warnings.Add(warning);
                }
            }

            return warnings;
        }

        private static void AddToWarnings(IEnumerable<XElement> relationships, XElement warnings)
        {
            // duplication Relationship
            Dictionary<string, List<XElement>> relationshipDict = new Dictionary<string, List<XElement>>();

            foreach (XElement relationship in relationships)
            {
                string fromTo = string.Format("{0},{1}", relationship.Attribute(Glossary.RelationshipFrom).Value,
                    relationship.Attribute(Glossary.RelationshipTo).Value);
                if (relationshipDict.ContainsKey(fromTo))
                {
                    relationshipDict[fromTo].Add(relationship);
                    continue;
                }

                //
                Relationship reverse = relationship.CreateRelationship().Reverse();
                fromTo = string.Format("{0},{1}", reverse.From, reverse.To);
                if (relationshipDict.ContainsKey(fromTo))
                {
                    relationshipDict[fromTo].Add(relationship);
                    continue;
                }

                relationshipDict.Add(fromTo, new List<XElement>() { relationship });
            }
            foreach (KeyValuePair<string, List<XElement>> pair in relationshipDict)
            {
                if (pair.Value.Count > 1)
                {
                    int count = pair.Value.Count(p => p.Element(Glossary.Prime) != null);
                    if (count == 1) continue;
                    string message;
                    string[] fromToArr = pair.Key.Split(',');
                    if (count == 0)
                    {
                        message = string.Format(Messages.PrimeRelationship_Not_Specified, fromToArr[0], fromToArr[1]);
                    }
                    else
                    {
                        message = string.Format(Messages.Duplicate_PrimeRelationship, fromToArr[0], fromToArr[1]);
                    }

                    XElement warning = new XElement("Warning", message);
                    StringBuilder sb = new StringBuilder();
                    foreach (XElement relationship in pair.Value)
                    {
                        sb.AppendLine(relationship.ToString());
                    }
                    warning.Add(new XComment(sb.ToString()));
                    warnings.Add(warning);
                }
            }
        }


    }
}
