using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using XData.Data.Services;
using XData.WebApp.Models;

namespace XData.Web.Mvc.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            AccountValidationStatus result = WebSecurity.Login(model);
            if (result == AccountValidationStatus.Success)
            {
                return RedirectToLocal(returnUrl);
            }

            switch (result)
            {
                case AccountValidationStatus.Success:
                    break;
                case AccountValidationStatus.Invalidation:
                    ModelState.AddModelError("", "The user name or password is incorrect");
                    break;
                case AccountValidationStatus.Disabled:
                    ModelState.AddModelError("", "Your account has been disabled, Please contact the administrator");
                    break;
                case AccountValidationStatus.LockedOut:
                    ModelState.AddModelError("", "Your account has been locked, Please contact the administrator");
                    break;
                default:
                    break;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            WebSecurity.Logout();
            return RedirectToAction("Index", "Home");
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //
            Dictionary<PasswordValidationStatuses, object> dict = WebSecurity.ValidatePassword(model.NewPassword);
            if (dict.ContainsKey(PasswordValidationStatuses.RequireDigit))
            {
                ModelState.AddModelError("", "The password requires a numeric digit ('0' - '9').");
            }
            if (dict.ContainsKey(PasswordValidationStatuses.RequireLowercase))
            {
                ModelState.AddModelError("", "The password requires a lower case letter ('a' - 'z').");
            }
            if (dict.ContainsKey(PasswordValidationStatuses.RequireUppercase))
            {
                ModelState.AddModelError("", "The password requires an upper case letter ('A' - 'Z').");
            }
            if (dict.ContainsKey(PasswordValidationStatuses.RequireNonLetterOrDigit))
            {
                ModelState.AddModelError("", "The password requires a non-letter or digit character.");
            }
            if (dict.ContainsKey(PasswordValidationStatuses.RequiredLength))
            {
                ModelState.AddModelError("", string.Format("The password must be at least {0} characters long.", dict[PasswordValidationStatuses.RequiredLength]));
            }
            if (dict.Count != 0) return View(model);

            //
            AccountValidationStatus result = WebSecurity.ChangePassword(model);
            if (result == AccountValidationStatus.Success)
            {
                return RedirectToAction("ChangePasswordSuccessful");
            }

            switch (result)
            {
                case AccountValidationStatus.Success:
                    break;
                case AccountValidationStatus.Invalidation:
                    ModelState.AddModelError("", "The current password is incorrect");
                    break;
                case AccountValidationStatus.Disabled:
                    ModelState.AddModelError("", "Your account has been disabled, Please contact the administrator");
                    break;
                case AccountValidationStatus.LockedOut:
                    ModelState.AddModelError("", "Your account has been locked, Please contact the administrator");
                    break;
                default:
                    break;
            }

            return View(model);
        }

        public ActionResult ChangePasswordSuccessful()
        {
            return View();
        }

        //@Html.Action("Name","Account")
        public ActionResult Name()
        {
            string name = WebSecurity.User.Element("Name").Value;
            return Content(name);
        }


    }
}