using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Schema
{
    // .NET Framework 4.5
    public static partial class AttributeSchemaExtensions
    {
        // validationAttributes
        public static MaxLengthAttribute CreateMaxLengthAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "MaxLength");
            int length = int.Parse(attributeSchema.Element("Length").Value);
            return new MaxLengthAttribute(length);
        }

        public static MinLengthAttribute CreateMinLengthAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "MinLength");
            int length = int.Parse(attributeSchema.Element("Length").Value);
            return new MinLengthAttribute(length);
        }

        public static CreditCardAttribute CreateCreditCardAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "CreditCard");
            return new CreditCardAttribute();
        }

        public static EmailAddressAttribute CreateEmailAddressAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "EmailAddress");
            return new EmailAddressAttribute();
        }

        public static PhoneAttribute CreatePhoneAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "Phone");
            return new PhoneAttribute();
        }

        public static UrlAttribute CreateUrlAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "Url");
            return new UrlAttribute();
        }


    }
}
