using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;
using XData.Data.Element.Validation;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Components
{
    public class ElementValidator
    {
        internal protected ElementContext ElementContext { get; internal set; }

        private ElementQuerier _elementQuerier = null;
        protected ElementQuerier ElementQuerier
        {
            get
            {
                if (_elementQuerier == null)
                {
                    _elementQuerier = new ElementQuerier(ElementContext);
                }
                return _elementQuerier;
            }
        }

        // throw an ElementValidationException
        public virtual void ValidateNodeOnInserting(XElement node, XElement schema)
        {
        }

        // throw an ElementValidationException
        public virtual void ValidateNodeOnUpdating(XElement node, XElement schema)
        {
        }

        // throw an ElementValidationException
        public virtual void ValidateNodeOnDeleting(XElement node, XElement schema)
        {
            List<ValidationResult> validationResults = GetRelationshipValidationResult(node, schema);
            ElementValidationException exception = schema.GetElementValidationException(node, ElementState.Delete, validationResults);
            if (exception != null) throw exception;
        }

        protected List<ValidationResult> GetRelationshipValidationResult(XElement node, XElement schema)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();

            IEnumerable<ManyToOneRelationship> toOneRelationships = schema.GetToOneRelationships(node);
            if (toOneRelationships.Count() == 0) return validationResults;

            //
            XElement keySchema = schema.GetElementSchema(node.Name.LocalName).GetKeySchema();
            XElement nodeKey = keySchema.ExtractKey(node);
            foreach (XElement fieldSchema in keySchema.Elements())
            {
                string dataType = fieldSchema.Attribute("DataType").Value;
                string value = nodeKey.Element(fieldSchema.Name.LocalName).Value;
                value = FilterValueDecorator.Decorate(value, Type.GetType(dataType));
                nodeKey.Element(fieldSchema.Name.LocalName).Value = value;
            }

            //
            foreach (ManyToOneRelationship relat in toOneRelationships)
            {
                List<string> list = new List<string>();
                for (int i = 0; i < relat.FieldNames.Length; i++)
                {
                    string nodeFieldName = relat.RelatedFieldNames[i];
                    string nodeValue = nodeKey.Element(nodeFieldName).Value;
                    list.Add(relat.FieldNames[i] + " eq " + nodeValue);
                }
                string filter = string.Join(" and ", list);

                int count = ElementQuerier.GetCount(relat.ElementName, filter, schema);
                if (count > 0)
                {
                    validationResults.Add(new ValidationResult(string.Format("A link between {0} and {1}", node.Name.LocalName, relat.ElementName)));
                }
            }
            return validationResults;
        }

        public virtual IEnumerable<ValidationResult> GetValidationResultsOnCreate(XElement element, XElement schema)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();

            // ...

            return validationResults;
        }

        public virtual IEnumerable<ValidationResult> GetValidationResultsOnUpdate(XElement element, XElement schema)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();

            // ...

            return validationResults;
        }

        public virtual IEnumerable<ValidationResult> GetValidationResultsOnDelete(XElement element, XElement schema)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();

            // ...

            return validationResults;
        }


    }
}
