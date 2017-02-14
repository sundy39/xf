using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Client.Services;

namespace XData.Windows.Models
{
    public static class ClientSecurity
    {
        private static AccountService AccountService = new AccountService();

        public static XElement User
        {
            get; private set;
        }

        public static ValidationStatus Login(string userName, string password)
        {
            ValidationStatus result = AccountService.Login(userName, password);
            if (result == ValidationStatus.Success)
            {
                User = AccountService.GetUser(userName);
            }
            return result;
        }

        public static void Logout()
        {
            AccountService.Logout();
            User = null;
        }

        public static object ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            return AccountService.ChangePassword(oldPassword, newPassword, confirmPassword);
        }


    }
}
