using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.ModelBinding;
using System.Xml.Linq;
using XData.Data.Element.Validation;
using XData.Data.Objects;
using XData.Data.Schema;

namespace System.Web.Http
{
    public static class ModelStateDictionaryExtensions
    {
        public static Exception GetElementValidationException(this ModelStateDictionary modelState, XElement node)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>(); 
            foreach (KeyValuePair<string, ModelState> pair in modelState)
            {
                foreach (ModelError error in pair.Value.Errors)
                {
                    ValidationResult validationResult =   new ValidationResult(error.ErrorMessage, new string[] { pair.Key });
                    validationResults.Add(validationResult);
                }
            }             
            XElement schema = new XElement("Schema");
            schema.SetAttributeValue("Version", "ModelState");
            return schema.GetElementValidationException(node, ElementState.Update, validationResults);
        }


    }
}
