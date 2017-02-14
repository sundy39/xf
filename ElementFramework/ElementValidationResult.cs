using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Objects;

namespace XData.Data.Element.Validation
{
    [Serializable]
    public class ElementValidationError
    {
        public string XPath { get; internal set; }
        public string NodeName{ get; private set; }
        public string FieldName { get; private set; }
        public string ErrorMessage { get; private set; }

        public ElementValidationError(string xPath, string nodeName, string fieldName, string errorMessage)
        {
            XPath = xPath;
            NodeName = nodeName;
            FieldName = fieldName;
            ErrorMessage = errorMessage;
        }    

        public ElementValidationError(IEnumerable<string> memberNames, string errorMessage)
        {
            XPath = (memberNames.Count() == 0) ? string.Empty : memberNames.Aggregate((p, v) => string.Format("{0},{1}", p, v));
            ErrorMessage = errorMessage;
        }

        public XElement ToElement()
        {
            XElement validationError = new XElement("ValidationError");
            validationError.Add(new XElement("XPath", XPath));
            validationError.Add(new XElement("NodeName", NodeName));
            validationError.Add(new XElement("FieldName", FieldName));
            validationError.Add(new XElement("ErrorMessage", ErrorMessage));
            return validationError;
        }
    }

    [Serializable]
    public class ElementValidationResult
    {
        public XElement Element { get; private set; }
        public ElementState ElementState { get; private set; }
        public string SchemaVersion { get; private set; }
        public string SchemaName { get; private set; }
        public bool IsValid { get { return ValidationErrors.Count == 0; } }
        public ICollection<ElementValidationError> ValidationErrors { get; private set; }

        public ElementValidationResult(XElement element, IEnumerable<ElementValidationError> validationErrors,
            ElementState elementState, string schemaVersion, string schemaName)
        {
            Element = new XElement(element);
            ValidationErrors = validationErrors.ToList();
            ElementState = elementState;
            SchemaName = schemaName;
            SchemaVersion = schemaVersion;
        }

        public XElement ToElement()
        {
            XElement validationErrors = new XElement("ValidationErrors");
            foreach (var validationError in ValidationErrors)
            {
                if (string.IsNullOrWhiteSpace(validationError.XPath))
                {
                    validationError.XPath = "/" + Element.Name.LocalName;
                }
                validationErrors.Add(validationError.ToElement());
            }
            return validationErrors;
        }
    }

    public static class ElementValidationResultArrayExtensions
    {
        public static XElement ToElement(this ElementValidationResult[] results)
        {
            XElement validationErrors = new XElement("ValidationErrors");
            foreach (var result in results)
            {
                validationErrors.Add(result.ToElement().Elements());
            }
            return validationErrors;
        }
    }
}
