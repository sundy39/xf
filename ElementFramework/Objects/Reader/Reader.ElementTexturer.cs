using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Query;
using XData.Data.Schema;
using XData.Data.Resources;
using System.Diagnostics;

namespace XData.Data.Objects
{
    public partial class Reader
    {
        protected class ElementTexturer
        {
            private Dictionary<RelatedObject, XElement> _relatedDictionary = new Dictionary<RelatedObject, XElement>();

            protected RelatedObject[] RelatedObjects;
            protected XElement[] RelatedSets;

            protected XElement Element;
            protected XElement Schema;
            protected Database Database;

            public ElementTexturer(RelatedObject relatedObject, XElement element, XElement schema, Database database)
            {
                Element = element;
                Schema = schema;
                Database = database;
                Initialize(relatedObject);
                RelatedObjects = _relatedDictionary.Keys.ToArray();
                RelatedSets = _relatedDictionary.Values.ToArray();
            }

            protected void Initialize(RelatedObject relatedObject)
            {
                XElement query = new XElement(relatedObject.Element);
                query.Add(relatedObject.Select);
                string relatedSetName;
                if (relatedObject is RelatedSet)
                {
                    RelatedSet relatedSet = relatedObject as RelatedSet;
                    query.Add(relatedSet.OrderBys);
                    relatedSetName = relatedSet.SetName;
                }
                else // relatedObject is ReferenceElement
                {
                    relatedSetName = "SetOf" + relatedObject.ElementAlias;
                }

                //
                XElement newQuery = new XElement(query);
                XElement select = newQuery.Element("Select");
                List<string> additionalFieldList = new List<string>();
                foreach (string fieldName in relatedObject.ElementAsParentFields)
                {
                    if (select.Element(fieldName) == null)
                    {
                        select.Add(new XElement(fieldName));
                        additionalFieldList.Add(fieldName);
                    }
                }

                if (relatedObject.Filter != null)
                {
                    newQuery.Add(relatedObject.Filter);
                }

                //
                Dictionary<string, string> elementAsChildRelatedFieldAliases = new Dictionary<string, string>();
                foreach (string fieldName in relatedObject.ElementAsChildRelatedFields)
                {
                    //elementAsChildRelatedFieldAliases.Add(fieldName, "Alias_" + Guid.NewGuid().ToString("N"));
                    // ORACLE Name.Length must <= 30
                    elementAsChildRelatedFieldAliases.Add(fieldName, "A_" + Guid.NewGuid().ToString("N").Substring(2 + 2 + 10)); // 32 - 14 + 2 = 20
                }

                //
                XElement result = Database.GetRelatedSet(Element, relatedObject.ReversedFullRelationshipPath,
                    relatedObject.RelationshipPath.Relationships.Length, relatedObject.ElementAsChildRelatedElement, elementAsChildRelatedFieldAliases,
                    newQuery, Schema, relatedObject.ElementAlias, relatedSetName);

                foreach (XElement element in result.Elements())
                {
                    foreach (string fieldName in relatedObject.ElementAsParentFields)
                    {
                        element.SetAttributeValue(fieldName, element.Element(fieldName).Value);
                    }

                    //
                    foreach (string fieldName in additionalFieldList)
                    {
                        element.Element(fieldName).Remove();
                    }

                    //
                    foreach (var pair in elementAsChildRelatedFieldAliases)
                    {
                        string attrName = string.Format("{0}.{1}", relatedObject.ElementAsChildRelatedElement, pair.Key);
                        string pairValue;
                        if (element.Element(pair.Value) != null)
                        {
                            // SQL Server
                            pairValue = pair.Value;
                        }
                        else if (element.Element(pair.Value.ToUpper()) != null)
                        {
                            // Oracle
                            pairValue = pair.Value.ToUpper();
                        }
                        else
                        {
                            // MySQL
                            pairValue = pair.Value.ToLower();
                        }
                        element.SetAttributeValue(attrName, element.Element(pairValue).Value);
                        element.Element(pairValue).Remove();
                    }
                }

                _relatedDictionary.Add(relatedObject, result);

                //
                foreach (RelatedObject child in relatedObject.Children)
                {
                    Initialize(child);
                }
            }

            public XElement GetElement()
            {
                for (int i = RelatedObjects.Length - 2; i >= 0; i--)
                {
                    foreach (XElement element in RelatedSets[i].Elements())
                    {
                        // add [idx + 1] to [idx]
                        AddChildren(element, i);
                    }
                }
                XElement result = PackageChildrenAsElement(RelatedSets[0].Elements(), RelatedObjects[0]);
                return result;
            }

            protected XElement PackageChildrenAsElement(IEnumerable<XElement> elmts, RelatedObject childObject)
            {
                XElement result;
                int count = elmts.Count();
                if (count == 0)
                {
                    if (childObject is ReferenceElement)
                    {
                        result = new XElement(childObject.ElementAlias);
                    }
                    else
                    {
                        string setName = (childObject as RelatedSet).SetName;
                        result = new XElement(setName);
                    }
                }
                if (childObject is ReferenceElement)
                {
                    //Debug.Assert(count < 2);

                    result = new XElement(elmts.First());
                    result.RemoveAttributes();
                }
                else
                {
                    string setName = (childObject as RelatedSet).SetName;
                    result = new XElement(setName);
                    foreach (XElement child in elmts)
                    {
                        XElement newChild = new XElement(child);
                        newChild.RemoveAttributes();
                        result.Add(newChild);
                    }
                }
                return result;
            }

            protected void AddChildren(XElement parent, int index)
            {
                RelatedObject childObject = RelatedObjects[index + 1];
                IEnumerable<XElement> children = GetChildren(parent, index);
                XElement result = PackageChildrenAsElement(children, childObject);
                parent.Add(result);
            }

            protected IEnumerable<XElement> GetChildren(XElement parent, int index)
            {
                IEnumerable<XElement> children = RelatedSets[index + 1].Elements();
                RelatedObject childObject = RelatedObjects[index + 1];
                string[] parentFields = childObject.RelationshipPath.Relationships[0].FieldNames;

                for (int i = 0; i < parentFields.Length; i++)
                {
                    string parentValue = parent.Attribute(parentFields[i]).Value;
                    string childFieldName = string.Format("{0}.{1}", childObject.ElementAsChildRelatedElement, parentFields[i]);

                    children = children.Where(p => p.Attribute(childFieldName).Value == parentValue);
                }
                return children;
            }


        }
    }
}
