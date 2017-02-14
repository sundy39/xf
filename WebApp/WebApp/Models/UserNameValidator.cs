using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace XData.WebApp.Models
{
    public class UserNameValidator
    {
        public static ValidationResult Validate(object value, ValidationContext context)
        {
            if (value == null) return ValidationResult.Success;

            bool isValid = true;

            string userName = value.ToString();
            if (userName.Contains("@"))
            {
                isValid = false;
            }
            else
            {
                userName = userName.Replace("(", string.Empty).Replace(")", string.Empty).Replace("+", string.Empty).Replace("-", string.Empty).Trim();
                long result;
                if (long.TryParse(userName, out result)) isValid = false;
            }
            if (isValid)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult("The user name must contain at least one of alphabetical characters and not contain '@'.");
            }
        }
    }
}