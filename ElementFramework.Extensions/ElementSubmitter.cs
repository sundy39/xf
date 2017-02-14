using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element.Validation;
using XData.Data.Objects;

namespace XData.Data.Element
{
    public class ElementSubmitter
    {
        protected ElementContext ElementContext;

        public ElementSubmitter(ElementContext elementContext)
        {
            ElementContext = elementContext;
        }

        public XElement Create(XElement value, XElement schema, bool isValidate)
        {
            if (isValidate)
            {
                var results = ElementContext.GetValidationResults(ElementState.Create, value, null, schema);
                return GetValidationResults(results);
            }

            ElementContext.Create(value, schema);
            return value;
        }

        public XElement Delete(XElement value, XElement schema, bool isValidate)
        {
            if (isValidate)
            {
                var results = ElementContext.GetValidationResults(ElementState.Delete, value, null, schema);
                return GetValidationResults(results);
            }

            ElementContext.Delete(value, schema);
            return value;
        }

        public XElement Update(XElement value, XElement schema, bool isValidate)
        {
            if (isValidate)
            {
                var results = ElementContext.GetValidationResults(ElementState.Update, value, null, schema);
                return GetValidationResults(results);
            }

            ElementContext.Update(value, schema);
            return value;
        }

        public XElement Submit(XElement packet, bool isValidate)
        {
            if (isValidate)
            {
                var results = ElementContext.GetValidationResults(packet);
                return GetValidationResults(results);
            }

            ElementContext.SaveChanges(packet);
            return packet;
        }

        // return
        //<ValidationResults>

        //  <ValidationResult>

        //    <ValidationErrors>

        //      <ValidationError>
        //        <XPath>...</XPath>
        //        <ErrorMessage>...</ErrorMessage>
        //      </ValidationError>

        //    </ValidationErrors>

        //  </ValidationResult>

        //</ValidationResults>
        protected XElement GetValidationResults(ElementValidationResult[] results)
        {
            XElement validationResults = new XElement("ValidationResults");
            foreach (ElementValidationResult result in results)
            {
                validationResults.Add(new XElement("ValidationResult", result.ToElement()));
            }
            return validationResults;
        }


    }
}
