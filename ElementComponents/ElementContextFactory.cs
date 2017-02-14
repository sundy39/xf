using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Configuration;
using XData.Data.Element;
using XData.Data.Objects;

namespace XData.Data.Components
{
    public class ElementContextFactory
    {
        protected ElementValidator ElementValidator
        {
            get
            {
                return ConfigurationCreator.CreateElementValidator(ElementContext);
            }
        }

        protected CommonFieldsSetter CommonFieldsSetter
        {
            get
            {
                return ConfigurationCreator.CreateCommonFieldsSetter();
            }
        }

        protected DbLogSqlProvider DbLogSqlProvider
        {
            get
            {
                return ConfigurationCreator.CreateDbLogSqlProvider(ElementContext);
            }
        }

        protected CurrentUser CurrentUser
        {
            get
            {
                return ConfigurationCreator.CreateCurrentUser(ElementContext);
            }
        }

        protected readonly ElementContext ElementContext = new ElementContext();

        public virtual ElementContext Create()
        {
            ElementContext.Database.Inserting += Database_Inserting;
            ElementContext.Database.Inserted += Database_Inserted;
            ElementContext.Database.Deleting += Database_Deleting;
            ElementContext.Database.Updating += Database_Updating;
            ElementContext.Validating += ElementContext_Validating;
            return ElementContext;
        }

        protected virtual void Database_Inserting(object sender, InsertingEventArgs args)
        {
            if (ElementValidator != null)
            {
                ElementValidator.ValidateNodeOnInserting(args.Node, args.Schema);
            }

            if (CommonFieldsSetter != null)
            {
                CommonFieldsSetter.SetOnInserting(args.Node, CurrentUser.ToElement());
            }
        }

        protected virtual void Database_Inserted(object sender, InsertedEventArgs args)
        {
            if (DbLogSqlProvider != null)
            {
                string sql = DbLogSqlProvider.GenerateSqlOnInserted(args.Node, args.Schema, CurrentUser.ToElement());
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    args.After.Add(sql);
                }
            }
        }

        protected virtual void Database_Deleting(object sender, DeletingEventArgs args)
        {
            if (ElementValidator != null)
            {
                ElementValidator.ValidateNodeOnDeleting(args.Node, args.Schema);
            }

            if (DbLogSqlProvider != null)
            {
                string sql = DbLogSqlProvider.GenerateSqlOnDeleting(args.Node, args.Schema, CurrentUser.ToElement());
                args.Before.Add(sql);
            }
        }

        protected virtual void Database_Updating(object sender, UpdatingEventArgs args)
        {
            if (ElementValidator != null)
            {
                ElementValidator.ValidateNodeOnUpdating(args.Node, args.Schema);
            }

            if (CommonFieldsSetter != null)
            {
                CommonFieldsSetter.SetOnUpdating(args.Node, CurrentUser.ToElement());
            }

            if (DbLogSqlProvider != null)
            {
                string sql = DbLogSqlProvider.GenerateSqlOnUpdating(args.Node, args.Schema, CurrentUser.ToElement());
                args.After.Add(sql);
            }
        }

        protected virtual void ElementContext_Validating(object sender, ValidatingEventArgs args)
        {
            if (ElementValidator != null)
            {
                IEnumerable<ValidationResult> validationResults = new List<ValidationResult>();
                switch (args.ElementState)
                {
                    case ElementState.Create:
                        validationResults = ElementValidator.GetValidationResultsOnCreate(args.Element, args.Schema);
                        break;
                    case ElementState.Update:
                        validationResults = ElementValidator.GetValidationResultsOnUpdate(args.Element, args.Schema);
                        break;
                    case ElementState.Delete:
                        validationResults = ElementValidator.GetValidationResultsOnDelete(args.Element, args.Schema);
                        break;
                    default:
                        break;
                }
                foreach (ValidationResult validationResult in validationResults)
                {
                    args.ValidationResults.Add(validationResult);
                }
            }
        }


    }
}
