using System;
using System.Xml.Linq;
using XData.Data.Element.Validation;
using XData.Data.Objects;

namespace XData.Data.Element
{
    public partial class ElementContext
    {
        public event ValidatingEventHandler Validating;

        protected void OnValidating(ValidatingEventArgs args)
        {
            if (Validating != null)
            {
                Validating(this, args);
            }
        }

        public ElementValidationResult[] GetValidationResults(ElementState elementState, XElement element, XElement original)
        {
            return GetValidationResults(elementState, element, original, SchemaManager.PrimarySchema);
        }

        public ElementValidationResult[] GetValidationResults(ElementState elementState, XElement element, XElement original, string schemaName)
        {
            return GetValidationResults(elementState, element, original, SchemaManager.GetSchema(schemaName));
        }

        public ElementValidationResult[] GetValidationResults(ElementState elementState, XElement element, XElement original, XElement schema)
        {
            switch (elementState)
            {
                case ElementState.Create:
                    Writer.RegisterCreate(element, schema);
                    break;
                case ElementState.Update:
                    if (original == null)
                    {
                        Writer.RegisterUpdate(element, schema);
                    }
                    else
                    {
                        Writer.RegisterUpdate(element, original, schema);
                    }
                    break;
                case ElementState.Delete:
                    BeforeRegisterDelete(element, schema);
                    Writer.RegisterDelete(element, schema);
                    break;
                default:
                    break;
            }
            return Writer.GetValidationResults();
        }

        public ElementValidationResult[] GetValidationResults(XElement packet)
        {
            RegisterInWriter(packet);
            return Writer.GetValidationResults();
        }


    }
}
