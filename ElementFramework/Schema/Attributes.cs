using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Schema
{
    // knows Attributes

    // System.ComponentModel.DataAnnotations.KeyAttribute
    // System.ComponentModel.DataAnnotations.TimestampAttribute
    // System.ComponentModel.DataAnnotations.ConcurrencyCheckAttribute
    // System.ComponentModel.DefaultValueAttribute

    // System.ComponentModel.DataAnnotations.DisplayFormatAttribute

    // System.ComponentModel.DisplayNameAttribute
    // System.ComponentModel.DataAnnotations.DisplayAttribute

    // validationAttributes
    //System.ComponentModel.DataAnnotations.ValidationAttribute
    //  System.ComponentModel.DataAnnotations.CustomValidationAttribute
    //  System.ComponentModel.DataAnnotations.DataTypeAttribute
    //  System.ComponentModel.DataAnnotations.RangeAttribute
    //  System.ComponentModel.DataAnnotations.RegularExpressionAttribute
    //  System.ComponentModel.DataAnnotations.RequiredAttribute
    //  System.ComponentModel.DataAnnotations.StringLengthAttribute

    [Serializable]
    public class UtcDateTimeAttribute : Attribute
    {
    }

    [Serializable]
    public class PrimeAttribute : Attribute
    {
    }

    [Serializable]
    public class SequenceAttribute : Attribute
    {
        public string SequenceName { get; private set; }

        public SequenceAttribute(string sequenceName)
        {
            SequenceName = sequenceName;
        }
    }

    public static partial class AttributeSchemaExtensions
    {
        public static UtcDateTimeAttribute CreateUtcDateTimeAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "UtcDateTime");
            return new UtcDateTimeAttribute();
        }

        public static KeyAttribute CreateKeyAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "Key");
            return new KeyAttribute();
        }

        public static PrimeAttribute CreatePrimeAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "Prime");
            return new PrimeAttribute();
        }

        public static SequenceAttribute CreateSequenceAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "Sequence");
            string sequenceName = attributeSchema.Element("SequenceName").Value;
            return new SequenceAttribute(sequenceName);
        }

        // RowVersion
        public static TimestampAttribute CreateTimestampAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "Timestamp");
            return new TimestampAttribute();
        }

        public static ConcurrencyCheckAttribute CreateConcurrencyCheckAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "ConcurrencyCheck");
            return new ConcurrencyCheckAttribute();
        }

        public static DefaultValueAttribute CreateDefaultValueAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "DefaultValue");
            string value = attributeSchema.Element("Value").Value;
            return new DefaultValueAttribute(value);
        }

        //
        public static DisplayNameAttribute CreateDisplayNameAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "DisplayName");
            string displayName = attributeSchema.Element("DisplayName").Value;
            return new DisplayNameAttribute(displayName);
        }

        public static DisplayAttribute CreateDisplayAttribute(this XElement attributeSchema)
        {
            Debug.Assert(attributeSchema.Name.LocalName == "Display");
            var attribute = new DisplayAttribute();
            if (attributeSchema.Element("AutoGenerateField") != null)
            {
                attribute.AutoGenerateField = attributeSchema.Element("AutoGenerateField").Value.ToLower() == bool.TrueString.ToLower();
            }
            if (attributeSchema.Element("AutoGenerateFilter") != null)
            {
                attribute.AutoGenerateFilter = attributeSchema.Element("AutoGenerateFilter").Value.ToLower() == bool.TrueString.ToLower();
            }
            if (attributeSchema.Element("Description") != null)
            {
                attribute.Description = attributeSchema.Element("Description").Value;
            }
            if (attributeSchema.Element("GroupName") != null)
            {
                attribute.GroupName = attributeSchema.Element("GroupName").Value;
            }
            if (attributeSchema.Element("Name") != null)
            {
                attribute.Name = attributeSchema.Element("Name").Value;
            }
            if (attributeSchema.Element("Order") != null)
            {
                attribute.Order = int.Parse(attributeSchema.Element("Order").Value);
            }
            if (attributeSchema.Element("Prompt") != null)
            {
                attribute.Name = attributeSchema.Element("Prompt").Value;
            }
            if (attributeSchema.Element("ResourceType") != null)
            {
                attribute.ResourceType = Type.GetType(attributeSchema.Element("ResourceType").Value);
            }
            if (attributeSchema.Element("ShortName") != null)
            {
                attribute.Name = attributeSchema.Element("ShortName").Value;
            }
            return attribute;
        }

        // validationAttributes
        public static CustomValidationAttribute CreateCustomValidationAttribute(this XElement attributeSchema)
        {
            Type validatorType = Type.GetType(attributeSchema.Element("ValidatorType").Value);
            string method = attributeSchema.Element("Method").Value;
            var attribute = new CustomValidationAttribute(validatorType, method);
            SetValidationAttribute(attribute, attributeSchema);
            return attribute;
        }

        public static DataTypeAttribute CreateDataTypeAttribute(this XElement attributeSchema)
        {
            DataTypeAttribute attribute = null;
            if (attributeSchema.Element("CustomDataType") != null)
            {
                string customDataType = attributeSchema.Element("CustomDataType").Value;
                attribute = new DataTypeAttribute(customDataType);
            }
            else if (attributeSchema.Element("DataType") != null)
            {
                string dataType = attributeSchema.Element("DataType").Value;
                attribute = new DataTypeAttribute((DataType)Enum.Parse(typeof(DataType), dataType));
            }
            SetValidationAttribute(attribute, attributeSchema);
            return attribute;
        }

        public static RangeAttribute CreateRangeAttribute(this XElement attributeSchema, Type dataType = null)
        {
            RangeAttribute attribute;
            if (dataType == typeof(int))
            {
                int minimum = int.Parse(attributeSchema.Element("Minimum").Value);
                int maximum = int.Parse(attributeSchema.Element("Maximum").Value);
                attribute = new RangeAttribute(minimum, maximum);
            }
            else if (dataType == typeof(double))
            {
                double minimum = double.Parse(attributeSchema.Element("Minimum").Value);
                double maximum = double.Parse(attributeSchema.Element("Maximum").Value);
                attribute = new RangeAttribute(minimum, maximum);
            }
            else
            {
                Type type = Type.GetType(attributeSchema.Element("Type").Value);
                string minimum = attributeSchema.Element("Minimum").Value;
                string maximum = attributeSchema.Element("Maximum").Value;
                attribute = new RangeAttribute(type, minimum, maximum);
            }
            SetValidationAttribute(attribute, attributeSchema);
            return attribute;
        }

        public static RegularExpressionAttribute CreateRegularExpressionAttribute(this XElement attributeSchema)
        {
            string pattern = attributeSchema.Element("Pattern").Value;
            var attribute = new RegularExpressionAttribute(pattern);
            SetValidationAttribute(attribute, attributeSchema);
            return attribute;
        }

        //  AllowEmptyStrings   false(default)  true
        //  null                false           false
        //  string.Empty        false           true
        // "ABC"                true            true
        public static RequiredAttribute CreateRequiredAttribute(this XElement attributeSchema)
        {
            var attribute = new RequiredAttribute() { AllowEmptyStrings = false };
            if (attributeSchema.Element("AllowEmptyStrings") != null)
            {
                attribute.AllowEmptyStrings = bool.Parse(attributeSchema.Element("AllowEmptyStrings").Value.ToLower());
            }
            SetValidationAttribute(attribute, attributeSchema);
            return attribute;
        }

        public static DisplayFormatAttribute CreateDisplayFormatAttribute(this XElement attributeSchema)
        {
            var attribute = new DisplayFormatAttribute();
            if (attributeSchema.Element("ApplyFormatInEditMode") != null)
            {
                attribute.ApplyFormatInEditMode = bool.Parse(attributeSchema.Element("ApplyFormatInEditMode").Value.ToLower());
            }
            if (attributeSchema.Element("ConvertEmptyStringToNull") != null)
            {
                attribute.ConvertEmptyStringToNull = bool.Parse(attributeSchema.Element("ConvertEmptyStringToNull").Value.ToLower());
            }
            if (attributeSchema.Element("DataFormatString") != null)
            {
                attribute.DataFormatString = attributeSchema.Element("DataFormatString").Value;
            }
            if (attributeSchema.Element("HtmlEncode") != null)
            {
                attribute.HtmlEncode = bool.Parse(attributeSchema.Element("HtmlEncode").Value.ToLower());
            }
            if (attributeSchema.Element("NullDisplayText") != null)
            {
                attribute.NullDisplayText = attributeSchema.Element("NullDisplayText").Value;
            }
            return attribute;
        }

        public static StringLengthAttribute CreateStringLengthAttribute(this XElement attributeSchema)
        {
            int maximumLength = int.Parse(attributeSchema.Element("MaximumLength").Value);
            var attribute = new StringLengthAttribute(maximumLength);
            if (attributeSchema.Element("MinimumLength") != null)
            {
                attribute.MinimumLength = int.Parse(attributeSchema.Element("MinimumLength").Value);
            }
            SetValidationAttribute(attribute, attributeSchema);
            return attribute;
        }

        private static void SetValidationAttribute(ValidationAttribute attribute, XElement attributeSchema)
        {
            if (attributeSchema.Element("ErrorMessage") != null)
            {
                attribute.ErrorMessage = attributeSchema.Element("ErrorMessage").Value;
            }
            if (attributeSchema.Element("ErrorMessageResourceType") != null)
            {
                attribute.ErrorMessageResourceType = Type.GetType(attributeSchema.Element("ErrorMessageResourceType").Value);
            }
            if (attributeSchema.Element("ErrorMessageResourceName") != null)
            {
                attribute.ErrorMessageResourceName = attributeSchema.Element("ErrorMessageResourceName").Value;
            }
        }

    }
}
