using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
//using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Security;
using System.Xml.Linq;
using XData.Data.Services;

namespace XData.WebApp.Models
{
    public static class WebSecurity
    {
        private static AccountService AccountService
        {
            get { return new AccountService(); }
        }

        public static XElement User
        {
            get
            {
                //if (HttpContext.Current.User.Identity.IsAuthenticated)
                if (Thread.CurrentPrincipal.Identity.IsAuthenticated)
                {
                    //return AccountService.GetUser(HttpContext.Current.User.Identity.Name);
                    return AccountService.GetUser(Thread.CurrentPrincipal.Identity.Name);
                }
                return null;
            }
        }

        public static AccountValidationStatus Login(LoginModel model)
        {
            AccountValidationStatus result = AccountService.Validate(model.UserName, model.Password);
            if (result == AccountValidationStatus.Success)
            {
                AccountService.UpdateLockedOutState(model.UserName, true);
                FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
            }
            else
            {
                AccountService.UpdateLockedOutState(model.UserName, false);
            }
            return result;
        }

        public static void Logout()
        {
            FormsAuthentication.SignOut();
        }

        private const string Api_Verification_Token_Name = "__ApiVerificationToken";

        public static HttpResponseMessage GetApiVerificationToken()
        {
            string apiVerificationToken = AccountService.GenerateApiVerificationToken();
            string encrypted = AccountService.EncryptApiVerificationToken(apiVerificationToken);

            HttpResponseMessage response = new HttpResponseMessage();
            string content = new XElement(Api_Verification_Token_Name, apiVerificationToken).ToString();
            response.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/xml");

            CookieHeaderValue cookie = new CookieHeaderValue(Api_Verification_Token_Name, encrypted);
            response.Headers.AddCookies(new CookieHeaderValue[] { cookie });
            return response;
        }

        public static HttpResponseMessage Login(XElement value)
        {
            AccountValidationStatus result;
            string userName = value.Element("UserName").Value;
            string password = value.Element("Password").Value;
            bool rememberMe = bool.Parse(value.Element("RememberMe").Value);

            result = AccountService.Validate(userName, password);
            if (result == AccountValidationStatus.Success)
            {
                AccountService.UpdateLockedOutState(userName, true);
                FormsAuthentication.SetAuthCookie(userName, rememberMe);
            }
            else
            {
                AccountService.UpdateLockedOutState(userName, false);
            }
            HttpResponseMessage response = new HttpResponseMessage();
            string content = new XElement("Login", result.ToString()).ToString();
            response.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/xml");

            CookieHeaderValue cookie = new CookieHeaderValue(Api_Verification_Token_Name, string.Empty);
            cookie.MaxAge = TimeSpan.Zero;
            response.Headers.AddCookies(new CookieHeaderValue[] { cookie });
            return response;
        }

        public static void ValidateApiVerificationToken(HttpRequestMessage request, XElement value)
        {
            try
            {
                string apiVerificationToken = value.Element(Api_Verification_Token_Name).Value;
                string encrypted = AccountService.EncryptApiVerificationToken(apiVerificationToken);
                string cookie = request.Headers.GetCookies("__ApiVerificationToken").First()["__ApiVerificationToken"].Value;
                if (encrypted == cookie) return;
                throw new Exception();
            }
            catch
            {
                throw new Exception("A required anti-forgery token was not supplied or was invalid.");
            }
        }

        public static Dictionary<PasswordValidationStatuses, object> ValidatePassword(string password)
        {
            return AccountService.ValidatePassword(password);
        }

        public static AccountValidationStatus ChangePassword(ChangePasswordModel model)
        {
            //string userName = HttpContext.Current.User.Identity.Name;
            string userName = Thread.CurrentPrincipal.Identity.Name;
            string password = model.OldPassword;

            AccountValidationStatus result = AccountService.Validate(userName, password);
            if (result == AccountValidationStatus.Success)
            {
                AccountService.SetPassword(userName, model.NewPassword);
            }

            return result;
        }

        public static HttpResponseMessage ChangePassword(XElement value, ApiController apiController)
        {
            ChangePasswordModel model = new ChangePasswordModel()
            {
                OldPassword = value.Element("OldPassword").Value,
                NewPassword = value.Element("NewPassword").Value,
                ConfirmPassword = value.Element("ConfirmPassword").Value
            };
            apiController.Validate(model);
            ModelStateDictionary modelState = apiController.ModelState;
            if (modelState.IsValid)
            {
                Dictionary<PasswordValidationStatuses, object> dict = ValidatePassword(model.NewPassword);
                string result;
                if (dict.Count == 0)
                {
                    AccountValidationStatus status = WebSecurity.ChangePassword(model);
                    result = status.ToString();
                }
                else
                {
                    List<string> list = new List<string>();
                    if (dict.ContainsKey(PasswordValidationStatuses.RequireDigit))
                    {
                        list.Add(PasswordValidationStatuses.RequireDigit.ToString() + ":" + true);
                    }
                    if (dict.ContainsKey(PasswordValidationStatuses.RequireLowercase))
                    {
                        list.Add(PasswordValidationStatuses.RequireLowercase.ToString() + ":" + true);
                    }
                    if (dict.ContainsKey(PasswordValidationStatuses.RequireUppercase))
                    {
                        list.Add(PasswordValidationStatuses.RequireUppercase.ToString() + ":" + true);
                    }
                    if (dict.ContainsKey(PasswordValidationStatuses.RequireNonLetterOrDigit))
                    {
                        list.Add(PasswordValidationStatuses.RequireNonLetterOrDigit.ToString() + ":" + true);
                    }
                    if (dict.ContainsKey(PasswordValidationStatuses.RequiredLength))
                    {
                        list.Add(PasswordValidationStatuses.RequiredLength.ToString() + ":" + dict[PasswordValidationStatuses.RequiredLength]);
                    }
                    result = string.Join("|", list);
                }
                HttpResponseMessage response = new HttpResponseMessage();
                string content = new XElement("ChangePassword", result).ToString();
                response.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/xml");
                return response;
            }

            throw modelState.GetElementValidationException(value);
        }


    }
}