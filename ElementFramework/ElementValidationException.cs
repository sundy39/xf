using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XData.Data.Element.Validation
{
    [Serializable]
    public class ElementValidationException : Exception, IElementException
    {
        public ElementValidationResult[] ValidationErrors { get; private set; }

        public ElementValidationException(string message, ElementValidationResult[] elementValidationResults)
            : base(message)
        {
            ValidationErrors = elementValidationResults;
        }

        public ElementValidationException(string message, ElementValidationResult[] elementValidationResults, Exception innerException)
            : base(message, innerException)
        {
            ValidationErrors = elementValidationResults;
        }

        public XElement ToElement()
        {
            XElement error = new XElement("Error");
            error.Add(new XElement("ExceptionMessage", Message));
            error.Add(new XElement("ExceptionType", this.GetType().FullName));

            //
            error.Add(ValidationErrors.ToElement());
            return error;
        }


    }
}
