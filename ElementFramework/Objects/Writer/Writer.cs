using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Element;
using XData.Data.Element.Validation;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public partial class Writer
    {
        protected readonly List<ElementUnit> ElementUnits = new List<ElementUnit>();

        public event ValidatingEventHandler Validating;

        protected void OnValidating(ValidatingEventArgs args)
        {
            if (Validating != null)
            {
                Validating(this, args);
            }
        }

        protected readonly Database Database;

        public Writer(Database database)
        {
            Database = database;
        }

        public void RegisterCreate(XElement element, XElement schema)
        {
            if (schema.IsSet(element))
            {
                foreach (XElement elmt in element.Elements())
                {
                    ElementUnit elementUnit = new ElementUnit(elmt, ElementState.Create, schema);
                    ElementUnits.Add(elementUnit);
                }
            }
            else
            {
                ElementUnit elementUnit = new ElementUnit(element, ElementState.Create, schema);
                ElementUnits.Add(elementUnit);
            }
        }

        public void RegisterDelete(XElement element, XElement schema)
        {
            XElement elementSchema = schema.GetElementSchemaBySetName(element.Name.LocalName);

            if (schema.IsSet(element))
            {
                foreach (XElement elmt in element.Elements())
                {
                    ElementUnit elementUnit = new ElementUnit(elmt, ElementState.Delete, schema);
                    ElementUnits.Add(elementUnit);
                }
            }
            else
            {
                ElementUnit elementUnit = new ElementUnit(element, ElementState.Delete, schema);
                ElementUnits.Add(elementUnit);
            }
        }

        public void RegisterUpdate(XElement element, XElement schema)
        {
            if (schema.IsSet(element))
            {
                XElement elementSchema = schema.GetElementSchemaBySetName(element.Name.LocalName);

                foreach (XElement elmt in element.Elements())
                {
                    XElement key = elementSchema.GetKeySchema().ExtractKey(elmt);
                    ElementUnit elementUnit = new ElementUnit(elmt, null, ElementState.Update, schema);
                    ElementUnits.Add(elementUnit);
                }
            }
            else
            {
                XElement elementSchema = schema.GetElementSchema(element.Name.LocalName);
                ElementUnit elementUnit = new ElementUnit(element, null, ElementState.Update, schema);
                ElementUnits.Add(elementUnit);
            }
        }

        public void RegisterUpdate(XElement element, XElement original, XElement schema)
        {
            if (schema.IsSet(element))
            {
                XElement elementSchema = schema.GetElementSchemaBySetName(element.Name.LocalName);

                foreach (XElement elmt in element.Elements())
                {
                    XElement key = elementSchema.GetKeySchema().ExtractKey(elmt);
                    XElement orig = original.Elements().Filter(key).FirstOrDefault();
                    if (orig == null)
                    {
                        throw new ArgumentException(string.Format(Messages.Not_Found_Original, elmt.ToString()));
                    }
                    ElementUnit elementUnit = new ElementUnit(elmt, orig, ElementState.Update, schema);
                    ElementUnits.Add(elementUnit);
                }
            }
            else
            {
                XElement elementSchema = schema.GetElementSchema(element.Name.LocalName);
                XElement key = elementSchema.GetKeySchema().ExtractKey(element);
                XElement origKey = elementSchema.GetKeySchema().ExtractKey(original);
                if (!XNode.DeepEquals(key, origKey))
                {
                    throw new ArgumentException(Messages.Validation_Element_Not_Match_Original);
                }
                ElementUnit elementUnit = new ElementUnit(element, original, ElementState.Update, schema);
                ElementUnits.Add(elementUnit);
            }
        }

        public ElementValidationResult[] GetValidationResults()
        {
            List<ElementValidationResult> validationResults = new List<ElementValidationResult>();
            foreach (ElementUnit elementUnit in ElementUnits)
            {
                ElementValidationResult validationResult = elementUnit.GetValidationResult();

                // 
                var args = new ValidatingEventArgs(elementUnit.Element, elementUnit.ElementState, elementUnit.Schema);
                OnValidating(args);
                foreach (ValidationResult argsResult in args.ValidationResults)
                {
                    if (argsResult == ValidationResult.Success) continue;
                    validationResult.ValidationErrors.Add(new ElementValidationError(argsResult.MemberNames, argsResult.ErrorMessage));
                }

                //
                if (validationResult.IsValid) continue;
                validationResults.Add(validationResult);
            }
            return validationResults.ToArray();
        }

        public void Validate()
        {
            ElementValidationResult[] validationResults = GetValidationResults();
            if (validationResults.Length > 0)
                throw new ElementValidationException(Messages.Validation_Error, validationResults);
        }

        public void SaveChanges()
        {
            try
            {
                Validate();

                //
                ConnectionState state = Database.Connection.State;
                try
                {
                    if (state == ConnectionState.Closed)
                    {
                        Database.Connection.Open();
                        Database.Transaction = Database.Connection.BeginTransaction();
                    }

                    foreach (ElementUnit elementUnit in ElementUnits)
                    {
                        switch (elementUnit.ElementState)
                        {
                            case ElementState.Create:
                                Database.Create(elementUnit.Element, elementUnit.Schema);
                                break;
                            case ElementState.Update:
                                if (elementUnit.Original == null)
                                {
                                    Database.Update(elementUnit.Element, elementUnit.Schema);
                                }
                                else
                                {
                                    Database.Update(elementUnit.Element, elementUnit.Original, elementUnit.Schema);
                                }
                                break;
                            case ElementState.Delete:
                                Database.Delete(elementUnit.Element, elementUnit.Schema);
                                break;
                            default:
                                break;
                        }
                    }

                    if (state == ConnectionState.Closed)
                    {
                        if (Database.Transaction != null) Database.Transaction.Commit();
                    }
                }
                catch
                {
                    if (state == ConnectionState.Closed)
                    {
                        if (Database.Transaction != null) Database.Transaction.Rollback();
                    }
                    throw;
                }
                finally
                {
                    if (state == ConnectionState.Closed)
                    {
                        Database.Connection.Close();
                        Database.Transaction = null;
                    }
                }
            }
            finally
            {
                ElementUnits.Clear();
            }
        }


    }
}
