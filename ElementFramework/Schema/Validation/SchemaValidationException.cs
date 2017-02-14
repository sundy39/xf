using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Schema.Validation
{
    public class SchemaValidationException : Exception, IElementException
    {
        public string Version { get; private set; }
        public string Name { get; private set; }
        public IEnumerable<ValidationResult> Errors { get; private set; }

        public SchemaValidationException(string errorMessage, IEnumerable<string> memberNames)
            : base(errorMessage)
        {
            List<ValidationResult> errorList = new List<ValidationResult>();
            errorList.Add(new ValidationResult(errorMessage, memberNames));
            Errors = errorList;
        }

        public SchemaValidationException(string version, IEnumerable<ValidationResult> validationResults)
            : base(string.Format(Messages.PrimarySchema_Verify_Failed, version))
        {
            Version = version;
            Errors = validationResults;

        }

        public SchemaValidationException(string version, string name, IEnumerable<ValidationResult> validationResults)
            : base(string.Format(Messages.NamedSchema_Verification_Failed, name, version))
        {
            Version = version;
            Name = name;
            Errors = validationResults;

        }

        public XElement ToElement()
        {
            XElement error = new XElement("Error");
            error.Add(new XElement("ExceptionMessage", Message));
            error.Add(new XElement("ExceptionType", this.GetType().FullName));

            //
            XElement schema = new XElement("Schema");
            schema.SetAttributeValue("Version", Version);
            if (!string.IsNullOrWhiteSpace(Name))
            {
                schema.SetAttributeValue("Name", Name);
            }
            foreach (var err in Errors)
            {
                schema.SetElementValue("ErrorMessage", err.ErrorMessage);
            }
            error.Add(schema);
            return error;
        }


    }
}
