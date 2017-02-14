using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element.Validation;
using XData.Data.Objects;

namespace XData.Data.Schema
{
    public static class SchemaExtensions2
    {
        public static IEnumerable<ManyToOneRelationship> GetToOneRelationships(this XElement schema, XElement node)
        {
            List<ManyToOneRelationship> toOneRelationships = new List<ManyToOneRelationship>();

            var toOnes = schema.Elements("Relationship")
                .Where(x => x.Attribute("To").Value == node.Name.LocalName)
                .Where(x => x.Attribute("Type").Value == "ManyToOne" || x.Attribute("Type").Value == "OneToOne");
            foreach (XElement toOne in toOnes)
            {
                toOneRelationships.Add(new ManyToOneRelationship(toOne));
            }

            var oneTos = schema.Elements("Relationship")
                .Where(x => x.Attribute("From").Value == node.Name.LocalName)
                .Where(x => x.Attribute("Type").Value == "OneToMany" || x.Attribute("Type").Value == "OneToOne");
            foreach (XElement oneTo in oneTos)
            {
                toOneRelationships.Add(new OneToManyRelationship(oneTo).Reverse() as ManyToOneRelationship);
            }

            return toOneRelationships;
        }

        // overload
        public static ElementValidationException GetElementValidationException(this XElement schema, XElement node, ElementState elementState, ValidationResult validationResult)
        {
            return GetElementValidationException(schema, node, elementState, new List<ValidationResult>() { validationResult });
        }

        public static ElementValidationException GetElementValidationException(this XElement schema, XElement node, ElementState elementState, IEnumerable<ValidationResult> validationResults)
        {
            List<ElementValidationError> validationErrors = new List<ElementValidationError>();
            foreach (ValidationResult result in validationResults)
            {
                validationErrors.Add(new ElementValidationError(result.MemberNames, result.ErrorMessage));
            }
            return GetElementValidationException(schema, node, elementState, validationErrors);
        }

        private static ElementValidationException GetElementValidationException(XElement schema, XElement node, ElementState elementState, IEnumerable<ElementValidationError> validationErrors)
        {
            if (validationErrors.Count() == 0) return null;

            string schemaVersion = schema.Attribute("Version").Value;
            string schemaName = null;
            if (schema.Attribute("Name") != null)
            {
                schemaName = schema.Attribute("Name").Value;
            }
            ElementValidationResult elementValidationResult = new ElementValidationResult(node, validationErrors, elementState, schemaVersion, schemaName);
            return new ElementValidationException("Validation error.", new ElementValidationResult[] { elementValidationResult });
        }


    }
}
