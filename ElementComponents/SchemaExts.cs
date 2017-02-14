using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public static class SchemaExts
    {
        // ManyToOneRelationship
        public static void SetRelatedValue(this XElement schema, XElement element, XElement related)
        {
            SimpleRelationship relationship = schema.CreateSimpleRelationship(element.Name.LocalName, related.Name.LocalName);
            if (relationship == null || !(relationship is ManyToOneRelationship))
            {
                throw new NullReferenceException(string.Format("Not found any ManyToOneRelationship from {0} to {1}.", element.Name.LocalName, related.Name.LocalName));
            }
            for (int i = 0; i < relationship.FieldNames.Length; i++)
            {
                string fieldName = relationship.FieldNames[i];
                string relatedFieldName = relationship.RelatedFieldNames[i];
                element.SetElementValue(fieldName, related.Element(relatedFieldName).Value);
            }
        }
    }
}
