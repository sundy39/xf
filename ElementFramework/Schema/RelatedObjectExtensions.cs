using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Schema
{
    public static class RelatedObjectExtensions
    {
        public static IEnumerable<RelatedObject> GetRelatedObjects(this XElement schema, string elementName)
        {
            XElement elementSchema = schema.GetElementSchema(elementName);
            List<RelatedObject> objList = new List<RelatedObject>();
            foreach (XElement fieldSchema in elementSchema.Elements())
            {
                if (fieldSchema.Attribute(Glossary.Element) != null && fieldSchema.Attribute(Glossary.Field) == null)
                {
                    objList.Add(RelatedObject.Create(fieldSchema, elementSchema.Name.LocalName, schema));
                }
            }
            return objList;
        }

        public static IEnumerable<string> GetElementAsParentFields(this IEnumerable<RelatedObject> relatedObjects)
        {
            List<string> list = new List<string>();
            foreach (RelatedObject obj in relatedObjects)
            {
                list.AddRange(obj.RelationshipPath.Relationships[0].FieldNames);
            }
            return list.Distinct();
        }


    }
}
