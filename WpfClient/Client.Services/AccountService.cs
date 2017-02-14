using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Client.Components;

namespace XData.Client.Services
{
    public enum ValidationStatus { Success, Invalidation, Disabled, LockedOut }

    [Flags]
    public enum PasswordValidationStatuses
    {
        RequiredLength = 0x1,
        RequireDigit = 0x2,
        RequireLowercase = 0x4,
        RequireUppercase = 0x8,
        RequireNonLetterOrDigit = 0x10,
    }

    public class AccountService
    {
        protected static List<ApiClient> Logined = new List<ApiClient>();

        protected ApiClient ApiClient = ApiClientManager.GetApiClient();

        // /User
        public ValidationStatus Login(string userName, string password)
        {
            string relativeUri = "/User";
            bool rememberMe = false;
            string value = ApiClient.Login(relativeUri, userName, password, rememberMe);
            ValidationStatus result = (ValidationStatus)Enum.Parse(typeof(ValidationStatus), value);
            if (result == ValidationStatus.Success)
            {
                if (!Logined.Contains(ApiClient))
                {
                    Logined.Add(ApiClient);
                }
            }
            return result;
        }

        // /User
        public void Logout()
        {
            if (Logined.Contains(ApiClient))
            {
                string relativeUri = "/User";
                ApiClient.Logout(relativeUri);
                Logined.Remove(ApiClient);
            }
        }

        // /XData/Users
        public XElement GetUser(string userName)
        {
            string relativeUri = string.Format("/XData/Users?$select=Id,EmployeeId,UserName,Employee.Name&$filter=LoweredUserName eq '{0}'", userName.ToLower());
            return ApiClient.Get(relativeUri).Elements().First();
        }

        // /User
        public object ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            string result = ApiClient.ChangePassword("/User", oldPassword, newPassword, confirmPassword);
            if (result.Contains(":") || result.Contains("|"))
            {
                Dictionary<PasswordValidationStatuses, object> dict = new Dictionary<PasswordValidationStatuses, object>();
                string[] items = result.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in items)
                {
                    string[] keyValue = item.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    PasswordValidationStatuses statuses = (PasswordValidationStatuses)Enum.Parse(typeof(PasswordValidationStatuses), keyValue[0]);                    
                    dict.Add(statuses, keyValue[1]);
                }
                return dict;
            }
            else
            {
                ValidationStatus status = (ValidationStatus)Enum.Parse(typeof(ValidationStatus), result);
                return status;
            }
        }


    }
}
