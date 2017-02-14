using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Element.Validation;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public partial class Writer
    {
        // IsElement is true
        protected class ElementUnit
        {
            public XElement Element { get; private set; }
            public XElement Original { get; private set; }
            public ElementState ElementState { get; private set; }
            public XElement Schema { get; private set; }

            public ElementUnit(XElement element, ElementState elementState, XElement schema)
            {
                Element = element;
                ElementState = elementState;
                Schema = schema;
            }

            public ElementUnit(XElement element, XElement original, ElementState elementState, XElement schema)
                : this(element, elementState, schema)
            {
                Original = original;
            }

            public virtual ElementValidationResult GetValidationResult()
            {
                string schemaVersion = Schema.Attribute("Version").Value;
                string schemaName = null;
                if (Schema.Attribute("Name") != null)
                {
                    schemaName = Schema.Attribute("Name").Value;
                }
                List<ElementValidationError> validationErrors = GetValidationErrors();
                return new ElementValidationResult(Element, validationErrors, ElementState, schemaVersion, schemaName);
            }

            protected virtual List<ElementValidationError> GetValidationErrors()
            {
                List<ElementValidationError> validationErrors = new List<ElementValidationError>();
                switch (ElementState)
                {
                    case ElementState.Create:
                        ValidateCreate("/" + Element.Name.LocalName, Element, validationErrors, null);
                        break;
                    case ElementState.Delete:
                        Stack<ElementValidationError> validationStack = new Stack<ElementValidationError>();
                        ValidateDelete("/" + Element.Name.LocalName, Element, validationStack);
                        int count = validationStack.Count;
                        for (int i = 0; i < count; i++)
                        {
                            validationErrors.Add(validationStack.Pop());
                        }
                        break;
                    case ElementState.Update:
                        ValidateUpdate("/" + Element.Name.LocalName, Element, validationErrors, null);
                        break;
                }

                return validationErrors;
            }

            protected virtual void ValidateCreate(string xPath, XElement node, List<ElementValidationError> validationErrors, Relationship relationship)
            {
                ValidateInternalParents(xPath, node, validationErrors);

                //
                ValidateCreateKey(xPath, node, validationErrors, relationship);
                ValidateCreateTimestamp(xPath, node, validationErrors);

                validationErrors.AddRange(GetAnnotationValidationErrors(xPath, node));

                foreach (XElement children in node.Elements().Where(p => p.HasElements))
                {
                    if (!Schema.IsSet(children)) continue;

                    ElementValidationError childValidationError;
                    Relationship childRelationship = CreateRelationship(xPath, node, children, out childValidationError);
                    if (childValidationError != null)
                    {
                        validationErrors.Add(childValidationError);
                        continue;
                    }

                    int i = 0;
                    bool isOnlyOne = children.Elements().Count() == 1;
                    foreach (XElement child in children.Elements())
                    {
                        if (Schema.IsElement(child))
                        {
                            i++;
                            string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, i);

                            if (childRelationship is ManyToManyRelationship)
                            {
                                var childKeyValidationError = GetKeyValidationError(path, child);
                                if (childKeyValidationError != null) validationErrors.Add(childKeyValidationError);
                            }
                            else
                            {
                                ValidateParentChild(path, child, node, validationErrors, childRelationship);
                                ValidateCreate(path, child, validationErrors, childRelationship);
                            }
                        }
                    }
                }
            }

            protected void ValidateInternalParents(string xPath, XElement node, List<ElementValidationError> validationErrors)
            {
                foreach (XElement related in node.Elements().Where(p => p.HasElements))
                {
                    if (!Schema.IsElement(related)) continue;

                    Relationship relationship = Schema.CreateToOneRelationship(node.Name.LocalName, related.Name.LocalName);
                    if (relationship == null) return;
                    if (relationship is ManyToOneRelationship || relationship is OneToOneRelationship)
                    {
                        SimpleRelationship simpleRelationship = relationship as SimpleRelationship;
                        for (int i = 0; i < simpleRelationship.FieldNames.Length; i++)
                        {
                            XElement nodeField = node.Element(simpleRelationship.FieldNames[i]);
                            XElement relatedField = related.Element(simpleRelationship.RelatedFieldNames[i]);
                            if (nodeField == null || string.IsNullOrWhiteSpace(nodeField.Value) || nodeField.Value == relatedField.Value)
                            {
                                ElementValidationError error = new ElementValidationError(xPath, node.Name.LocalName, nodeField.Name.LocalName,
                                    string.Format(Messages.Validation_RelatedFields_Not_Match,
                                        relatedField.Name.LocalName, related.Name.LocalName,
                                        nodeField.Name.LocalName, node.Name.LocalName));
                                validationErrors.Add(error);
                            }
                        }
                    }
                }
            }

            protected void ValidateParentChild(string xPath, XElement child, XElement node, List<ElementValidationError> validationErrors, Relationship childRelationship)
            {
                // ManyToOne or OneToOne
                SimpleRelationship relationship = childRelationship as SimpleRelationship;
                for (int i = 0; i < relationship.FieldNames.Length; i++)
                {
                    XElement childField = child.Element(relationship.FieldNames[i]);
                    if (childField == null) continue;
                    if (string.IsNullOrWhiteSpace(childField.Value)) continue;
                    XElement nodeField = node.Element(relationship.RelatedFieldNames[i]);
                    if (nodeField == null || string.IsNullOrWhiteSpace(nodeField.Value) || childField.Value != nodeField.Value)
                    {
                        ElementValidationError error = new ElementValidationError(xPath, node.Name.LocalName, nodeField.Name.LocalName,
                            string.Format(Messages.Validation_RelatedFields_Not_Match,
                                childField.Name.LocalName, child.Name.LocalName, nodeField.Name.LocalName, node.Name.LocalName));
                        validationErrors.Add(error);
                    }
                }
            }

            protected void ValidateCreateKey(string xPath, XElement node, List<ElementValidationError> validationErrors, Relationship relationship)
            {
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                XElement keySchema = elementSchema.GetKeySchema();
                if (keySchema.Elements().Any(p => p.Attribute("AutoIncrement") != null)) return;
                if (keySchema.Elements().Any(p => p.Element(Glossary.Sequence) != null)) return;
                string[] fieldNames = (relationship == null) ? new string[0] : (relationship as SimpleRelationship).FieldNames;
                List<string> fieldSchemaNames = new List<string>();
                foreach (XElement keyFieldSchema in keySchema.Elements())
                {
                    if (keyFieldSchema.Element("DefaultValue") == null)
                    {
                        if (node.Element(keyFieldSchema.Name) == null)
                        {
                            if (!fieldNames.Contains(keyFieldSchema.Name.LocalName))
                            {
                                fieldSchemaNames.Add(keyFieldSchema.Name.LocalName);
                            }
                        }
                    }
                }
                if (fieldSchemaNames.Count > 0)
                {
                    validationErrors.Add(new ElementValidationError(xPath, node.Name.LocalName, string.Empty,
                        string.Format(Messages.Validation_Invalid_Key, string.Join(",", fieldSchemaNames))));
                }
            }

            protected void ValidateCreateTimestamp(string xPath, XElement node, List<ElementValidationError> validationErrors)
            {
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                XElement timestampFieldSchema = elementSchema.Elements().FirstOrDefault(p => p.Element("Timestamp") != null);
                if (timestampFieldSchema == null) return;
                string sqlDbType = timestampFieldSchema.Attribute("SqlDbType").Value.ToLower();
                if (sqlDbType == "timestamp" || sqlDbType == "rowversion") return;
                if (timestampFieldSchema.Element("DefaultValue") != null) return;
                if (IsFieldValueNullOrWhiteSpace(timestampFieldSchema.Name, node))
                    validationErrors.Add(new ElementValidationError(xPath + "/" + timestampFieldSchema.Name.LocalName,
                        elementSchema.Name.LocalName, timestampFieldSchema.Name.LocalName,
                        Messages.Timestamp_IsNullOrEmpty));
            }

            protected bool IsFieldValueNullOrWhiteSpace(XName fieldName, XElement node)
            {
                bool isNullOrWhiteSpace = false;
                if (node.Element(fieldName) == null)
                {
                    isNullOrWhiteSpace = true;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(node.Element(fieldName).Value))
                    {
                        isNullOrWhiteSpace = true;
                    }
                }
                return isNullOrWhiteSpace;
            }

            // TurnInsideOut or CutOffOneToManyRelationships
            protected ElementValidationError GetKeyValidationError(string xPath, XElement node)
            {
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                XElement keySchema = elementSchema.GetKeySchema();
                List<string> fields = new List<string>();
                foreach (XElement keyFieldSchema in keySchema.Elements())
                {
                    if (IsFieldValueNullOrWhiteSpace(keyFieldSchema.Name, node))
                    {
                        fields.Add(keyFieldSchema.Name.LocalName);
                    }
                }
                if (fields.Count == 0) return null;
                return new ElementValidationError(xPath + "/" + string.Join(",", fields),
                    node.Name.LocalName, string.Empty, Messages.Validation_Invalid_Key);
            }

            // TurnInsideOut or CutOffOneToManyRelationships
            protected ElementValidationError GetTimestampValidationError(string xPath, XElement node)
            {
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                XElement timestampFieldSchema = elementSchema.Elements().FirstOrDefault(p => p.Element("Timestamp") != null);
                if (timestampFieldSchema == null) return null;
                if (IsFieldValueNullOrWhiteSpace(timestampFieldSchema.Name, node))
                    return new ElementValidationError(xPath + "/" + timestampFieldSchema.Name.LocalName,
                        elementSchema.Name.LocalName, timestampFieldSchema.Name.LocalName,
                        Messages.Timestamp_IsNullOrEmpty);
                return null;
            }

            protected virtual void ValidateDelete(string xPath, XElement node, Stack<ElementValidationError> validationStack)
            {
                List<ElementValidationError> validationErrors = new List<ElementValidationError>();
                ValidateInternalParents(xPath, node, validationErrors);
                validationErrors.Reverse();
                foreach (var error in validationErrors)
                {
                    validationStack.Push(error);
                }

                //
                var keyValidationError = GetKeyValidationError(xPath, node);
                if (keyValidationError != null) validationStack.Push(keyValidationError);
                var timestampValidationError = GetTimestampValidationError(xPath, node);
                if (timestampValidationError != null) validationStack.Push(timestampValidationError);

                foreach (XElement children in node.Elements().Where(p => p.HasElements))
                {
                    if (!Schema.IsSet(children)) continue;

                    ElementValidationError validationError;
                    Relationship childRelationship = CreateRelationship(xPath, node, children, out validationError);
                    if (validationError != null)
                    {
                        validationStack.Push(validationError);
                        continue;
                    }

                    int i = 0;
                    bool isOnlyOne = children.Elements().Count() == 1;
                    foreach (XElement child in children.Elements())
                    {
                        if (Schema.IsElement(child))
                        {
                            i++;
                            string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, i);

                            if (childRelationship is ManyToManyRelationship)
                            {
                                var childKeyValidationError = GetKeyValidationError(path, child);
                                if (childKeyValidationError != null) validationStack.Push(childKeyValidationError);
                            }
                            else
                            {
                                List<ElementValidationError> errors = new List<ElementValidationError>();
                                ValidateParentChild(path, child, node, errors, childRelationship);
                                errors.Reverse();
                                foreach (var error in errors)
                                {
                                    validationStack.Push(error);
                                }
                                ValidateDelete(path, child, validationStack);
                            }
                        }
                    }
                }
            }

            protected Relationship CreateRelationship(string xPath, XElement node, XElement childSet, out ElementValidationError validationError)
            {
                var childElementSchema = Schema.GetElementSchemaBySetName(childSet.Name.LocalName);
                var relationship = Schema.CreateRelationship(childElementSchema.Name.LocalName, node.Name.LocalName);
                if (relationship == null || relationship is OneToManyRelationship)
                {
                    validationError = new ElementValidationError(xPath + "/" + childElementSchema.Name.LocalName, node.Name.LocalName, string.Empty,
                        string.Format(Messages.Validation_No_Available_Relationship, childElementSchema.Name.LocalName, node.Name.LocalName));
                }
                else
                {
                    validationError = null;
                }
                return relationship;
            }

            protected bool? IsCreate(XElement node)
            {
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                XElement keySchema = elementSchema.GetKeySchema();
                XElement autoFieldSchema = keySchema.Elements().FirstOrDefault(p => p.Attribute("AutoIncrement") != null);
                if (autoFieldSchema == null)
                {
                    autoFieldSchema = keySchema.Elements().FirstOrDefault(p => p.Element(Glossary.Sequence) != null);
                }
                if (autoFieldSchema == null) return null;
                return node.Element(autoFieldSchema.Name) == null;
            }

            protected virtual void ValidateUpdate(string xPath, XElement node, List<ElementValidationError> validationErrors, Relationship relationship)
            {
                ValidateInternalParents(xPath, node, validationErrors);

                //
                bool? isCreate;
                if (relationship == null)
                {
                    // GetValidationErrors()
                    isCreate = false;
                }
                else
                {
                    // recursion
                    isCreate = IsCreate(node);
                }
                if (isCreate != null)
                {
                    if ((bool)isCreate)
                    {
                        ValidateCreateKey(xPath, node, validationErrors, relationship);
                        ValidateCreateTimestamp(xPath, node, validationErrors);
                    }
                    else
                    {
                        var keyValidationError = GetKeyValidationError(xPath, node);
                        if (keyValidationError != null) validationErrors.Add(keyValidationError);
                        var timestampValidationError = GetTimestampValidationError(xPath, node);
                        if (timestampValidationError != null) validationErrors.Add(timestampValidationError);
                    }
                }
                validationErrors.AddRange(GetAnnotationValidationErrors(xPath, node));

                foreach (XElement children in node.Elements().Where(p => p.HasElements))
                {
                    if (!Schema.IsSet(children)) continue;

                    ElementValidationError childValidationError;
                    Relationship childRelationship = CreateRelationship(xPath, node, children, out childValidationError);
                    if (childValidationError != null)
                    {
                        validationErrors.Add(childValidationError);
                        continue;
                    }

                    int i = 0;
                    bool isOnlyOne = children.Elements().Count() == 1;
                    foreach (XElement child in children.Elements())
                    {
                        if (Schema.IsElement(child))
                        {
                            i++;
                            string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName)
                                : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, i);

                            if (childRelationship is ManyToManyRelationship)
                            {
                                var childKeyValidationError = GetKeyValidationError(path, child);
                                if (childKeyValidationError != null) validationErrors.Add(childKeyValidationError);
                            }
                            else
                            {
                                ValidateParentChild(path, child, node, validationErrors, childRelationship);
                                ValidateUpdate(path, child, validationErrors, childRelationship);
                            }
                        }
                    }
                }
            }

            protected virtual List<ElementValidationError> GetAnnotationValidationErrors(string xPath, XElement node)
            {
                List<ElementValidationError> validationErrors = new List<ElementValidationError>();
                XElement elementSchema = Schema.GetElementSchema(node.Name.LocalName);
                foreach (XElement fieldSchema in elementSchema.Elements())
                {
                    if (node.Element(fieldSchema.Name) == null) continue;

                    foreach (ValidationAttribute validationAttribute in fieldSchema.CreateValidationAttributes())
                    {
                        object fieldValue = fieldSchema.GetObject(node.Element(fieldSchema.Name).Value);
                        if (!validationAttribute.IsValid(fieldValue))
                        {
                            string dispalyName = fieldSchema.GetDisplayName();
                            string errorMessage = validationAttribute.FormatErrorMessage(dispalyName);
                            ElementValidationError elementValidationError = new ElementValidationError(xPath + "/" + fieldSchema.Name.LocalName,
                                elementSchema.Name.LocalName, fieldSchema.Name.LocalName, errorMessage);
                            validationErrors.Add(elementValidationError);
                        }
                    }
                }
                return validationErrors;
            }


        }
    }
}
