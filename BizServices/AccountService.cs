using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using XData.Data.Components;
using XData.Data.Configuration;
using XData.Data.Element;
using XData.Data.Objects;
using XData.Data.Security;

namespace XData.Data.Services
{
    public enum AccountValidationStatus { Success, Invalidation, Disabled, LockedOut }

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
        protected ElementContext ElementContext = ConfigurationCreator.CreateElementContext();

        protected Database Database
        {
            get
            {
                return ElementContext.Database;
            }
        }

        protected static IEncryptor PasswordEncryptor = ConfigurationCreator.CreatePasswordEncryptor();

        private Authorizor _authorizor = null;
        protected Authorizor Authorizor
        {
            get
            {
                if (_authorizor == null)
                {
                    _authorizor = ConfigurationCreator.CreateAuthorizor(ElementContext);
                }
                return _authorizor;
            }
        }

        public bool IsAuthorized(string route, string verb, bool isAuthorized, string userName)
        {
            IEnumerable<string> roleNames = new RolesService().GetRoles(userName);
            return Authorizor.IsAuthorized(route, verb, isAuthorized, userName, roleNames);
        }

        public AccountValidationStatus Validate(string userName, string password)
        {
            DbParameter parameter;
            string sql = GetValidationSql(userName, out parameter);
            IEnumerable<XElement> result = Database.SqlQuery("User", sql, parameter);
            int count = result.Count();
            if (count == 0) return AccountValidationStatus.Invalidation;

            XElement user = result.First();
            if (user.Element("IsDisabled").Value == true.ToString()) return AccountValidationStatus.Disabled;
            if (user.Element("IsLockedOut").Value == true.ToString()) return AccountValidationStatus.LockedOut;
            if (ValidateAccount(user, password)) return AccountValidationStatus.Success;
            return AccountValidationStatus.Invalidation;
        }

        public void UpdateLockedOutState(string userName, bool isSuccessful)
        {
            XElement user = GetUser(userName);
            if (isSuccessful)
            {
                user.SetElementValue("LastLoginDate", DateTime.Now.ToCanonicalString());
                user.SetElementValue("FailedPasswordAttemptCount", 0);
            }
            else
            {
                ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
                AccountConfigurationElement configurationElement = configurationSection.Account as AccountConfigurationElement;
                if (configurationElement.MaxInvalidAttempts == 0) return;

                int failedCount = int.Parse(user.Element("FailedPasswordAttemptCount").Value);
                if (failedCount > 0)
                {
                    DateTime start = DateTime.Parse(user.Element("FailedPasswordAttemptWindowStart").Value);
                    if ((DateTime.Now - start).TotalMinutes < configurationElement.AttemptWindow)
                    {
                        if (failedCount >= configurationElement.MaxInvalidAttempts)
                        {
                            user.SetElementValue("IsLockedOut", true.ToString());
                            user.SetElementValue("LastLockoutDate", DateTime.Now.ToCanonicalString());
                        }
                        else
                        {
                            failedCount++;
                            user.SetElementValue("FailedPasswordAttemptCount", failedCount);
                        }
                    }
                    else
                    {
                        user.SetElementValue("FailedPasswordAttemptCount", 1);
                        user.SetElementValue("FailedPasswordAttemptWindowStart", DateTime.Now.ToCanonicalString());
                    }
                }
                else
                {
                    user.SetElementValue("FailedPasswordAttemptCount", 1);
                    user.SetElementValue("FailedPasswordAttemptWindowStart", DateTime.Now.ToCanonicalString());
                }
            }
            ElementContext.Update(user);
        }

        protected string GetValidationSql(string userName, out DbParameter parameter)
        {
            if (Database is SqlDatabase) // XData.Data.Objects.SqlDatabase
            {
                string sql = string.Format("SELECT * FROM Users WHERE LoweredUserName = @UserName", userName.ToLower());
                parameter = Database.CreateDbParameter("@UserName", userName);
                return sql;
            }

            string typeName = Database.GetType().FullName;
            if (typeName == "XData.Data.Objects.OracleDatabase")
            {
                string sql = string.Format("SELECT * FROM Users WHERE LoweredUserName = :UserName", userName.ToLower());
                parameter = Database.CreateDbParameter(":UserName", userName);
                return sql;
            }

            throw new NotSupportedException(typeName);
        }

        protected bool ValidateAccount(XElement user, string password)
        {
            int format = int.Parse(user.Element("PasswordFormat").Value);
            string passwordAlgorithm = user.Element("PasswordAlgorithm").Value;
            Guid algorithm = string.IsNullOrWhiteSpace(passwordAlgorithm) ? Guid.Empty : Guid.Parse(passwordAlgorithm);
            string salt = user.Element("PasswordSalt").Value;
            return user.Element("Password").Value == EncryptPassword(password, format, algorithm, salt);
        }

        protected string EncryptPassword(string password, int format, Guid algorithm, string salt)
        {
            if (format == (int)PasswordFormat.Clear) return password;
            if (format == (int)PasswordFormat.Hashed)
            {
                IEncryptor encryptor = ConfigurationCreator.Hashers[algorithm];
                return encryptor.Encrypt(password, salt);
            }
            if (format == (int)PasswordFormat.Encrypted)
            {
                IEncryptor encryptor = ConfigurationCreator.Cryptors[algorithm];
                return encryptor.Encrypt(password, salt);
            }

            throw new NotSupportedException(format.ToString());
        }

        public XElement GetUser(string userName)
        {
            DbParameter parameter;
            string sql = GetUserSql(userName, out parameter);
            IEnumerable<XElement> result = Database.SqlQuery("User", sql, parameter);
            return result.First();
        }

        protected string GetUserSql(string userName, out DbParameter parameter)
        {
            if (Database is SqlDatabase) // XData.Data.Objects.SqlDatabase
            {
                string sql =
@"SELECT U.Id, U.EmployeeId, U.UserName, E.Name FROM Users U LEFT JOIN Employees E ON U.EmployeeId = E.Id WHERE U.LoweredUserName = @LoweredUserName";

                parameter = Database.CreateDbParameter("@LoweredUserName", userName);
                return sql;
            }

            string typeName = Database.GetType().FullName;
            if (typeName == "XData.Data.Objects.OracleDatabase")
            {
                string sql =
@"SELECT U.Id, U.EmployeeId, U.UserName, E.Name FROM Users U LEFT JOIN Employees E ON U.EmployeeId = E.Id WHERE U.LoweredUserName = :LoweredUserName";

                parameter = Database.CreateDbParameter(":LoweredUserName", userName.ToLower());
                return sql;
            }

            throw new NotSupportedException(typeName);
        }

        public void SetPassword(string userName, string newPassword)
        {
            XElement user = GetUser(userName);
            int id = int.Parse(user.Element("Id").Value);
            SetPassword(id, newPassword, Database);
        }

        public Dictionary<PasswordValidationStatuses, object> ValidatePassword(string password)
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            PasswordConfigurationElement configurationElement = configurationSection.Password as PasswordConfigurationElement;

            Dictionary<PasswordValidationStatuses, object> dict = new Dictionary<PasswordValidationStatuses, object>();
            if (configurationElement.RequiredLength > 0)
            {
                if (password.Length < configurationElement.RequiredLength)
                {
                    dict.Add(PasswordValidationStatuses.RequiredLength, configurationElement.RequiredLength);
                }
            }

            bool hasDigit = false;
            bool hasLowercase = false;
            bool hasUppercase = false;
            bool hasNonLetterOrDigit = false;
            char[] array = password.ToCharArray();
            foreach (char c in array)
            {
                if (c >= '0' && c <= '9') { hasDigit = true; }
                else if (c >= 'a' && c <= 'z') { hasLowercase = true; }
                else if (c >= 'A' && c <= 'Z') { hasUppercase = true; }
                else { hasNonLetterOrDigit = true; }
            }
            if (configurationElement.RequireDigit)
            {
                if (!hasDigit) dict.Add(PasswordValidationStatuses.RequireDigit, true);
            }
            if (configurationElement.RequireLowercase)
            {
                if (!hasLowercase) dict.Add(PasswordValidationStatuses.RequireLowercase, true);
            }
            if (configurationElement.RequireUppercase)
            {
                if (!hasUppercase) dict.Add(PasswordValidationStatuses.RequireUppercase, true);
            }
            if (configurationElement.RequireNonLetterOrDigit)
            {
                if (!hasNonLetterOrDigit) dict.Add(PasswordValidationStatuses.RequireNonLetterOrDigit, true);
            }
            return dict;
        }

        internal protected static string Encrypt(string password, out int format, out Guid algorithm, out string salt)
        {
            string encrypted;
            if (PasswordEncryptor == null)
            {
                format = (int)PasswordFormat.Clear;
                algorithm = Guid.Empty;
                salt = string.Empty;
                encrypted = password;
            }
            else if (PasswordEncryptor is Hasher)
            {
                format = (int)PasswordFormat.Hashed;
                algorithm = PasswordEncryptor.GetType().GUID;
                salt = GenerateSalt();
                encrypted = PasswordEncryptor.Encrypt(password, salt);
            }
            else if (PasswordEncryptor is Cryptor)
            {
                format = (int)PasswordFormat.Encrypted;
                algorithm = PasswordEncryptor.GetType().GUID;
                salt = GenerateSalt();
                encrypted = PasswordEncryptor.Encrypt(password, salt);
            }
            else
            {
                throw new NotSupportedException(); // never
            }

            return encrypted;
        }

        internal protected static void SetPassword(int id, string newPassword, Database database)
        {
            int format;
            Guid algorithm;
            string salt;
            string encrypted = Encrypt(newPassword, out format, out algorithm, out salt);

            DbParameter[] parameter;
            string sql = GetSetPasswordSql(database, id, encrypted, format, algorithm, salt, out parameter);
            int i = database.ExecuteSqlCommand(sql, parameter);
        }

        protected static string GetSetPasswordSql(Database database, int id, string password, int format, Guid algorithm, string salt, out DbParameter[] parameter)
        {
            if (database is SqlDatabase)
            {
                string sql = @"
 UPDATE Users
 SET Password = @Password,
 PasswordFormat = @Format,
 PasswordAlgorithm = @Algorithm,
 PasswordSalt = @Salt,
 LastPasswordChangedDate = GETDATE()
 WHERE Id = {0}";
                sql = string.Format(sql, id);
                parameter = new DbParameter[4];
                parameter[0] = database.CreateDbParameter("@Password", password);
                parameter[1] = database.CreateDbParameter("@Format", format);
                parameter[2] = database.CreateDbParameter("@Algorithm", algorithm);
                parameter[3] = database.CreateDbParameter("@Salt", salt);
                return sql;
            }

            string typeName = database.GetType().FullName;
            if (typeName == "XData.Data.Objects.OracleDatabase")
            {
                string sql = @"
 UPDATE Users
 SET Password = :Password,
 PasswordFormat = :Format,
 PasswordAlgorithm = :Algorithm,
 PasswordSalt = :Salt,
 LastPasswordChangedDate = SYSDATE
 WHERE Id = {0}";
                sql = string.Format(sql, id);
                parameter = new DbParameter[4];
                parameter[0] = database.CreateDbParameter(":Password", password);
                parameter[1] = database.CreateDbParameter(":Format", format);
                parameter[2] = database.CreateDbParameter(":Algorithm", algorithm);
                parameter[3] = database.CreateDbParameter(":Salt", salt);
                return sql;
            }

            throw new NotSupportedException(typeName);
        }

        protected static string GenerateSalt()
        {
            return new SaltGenerator().GenerateSalt(16);
        }

        public string GenerateApiVerificationToken()
        {
            byte[] array = new byte[64];
            new RNGCryptoServiceProvider().GetBytes(array);
            return Convert.ToBase64String(array);
        }

        public string EncryptApiVerificationToken(string apiVerificationToken)
        {
            return new ApiVerificationTokenCryptor().Encrypt(apiVerificationToken);
        }

        protected class ApiVerificationTokenCryptor : RijndaelCryptor
        {
            protected const string KEY = "AhfReb3kNp86H1vdFnqSZnliaPQi6GvIdDjukiAig5s=";
            protected const string IV = "mqxvh+NGXcUdR31XtIFUlQ==";

            protected override string GetKey()
            {
                return KEY;
            }

            protected override string GetIV()
            {
                return IV;
            }

        }

    }
}