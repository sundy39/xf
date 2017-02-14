using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace XData.WebApp.Models
{
    public class SetUserNameModel
    {
        [Required]
        [StringLength(8, ErrorMessage = "The {0} must be at least {2} and not more than {1} characters long.", MinimumLength = 4)]
        [CustomValidation(typeof(UserNameValidator), "Validate")]
        [Display(Name = "New user name")]
        public string NewUserName { get; set; }

    }
}