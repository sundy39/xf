using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Configuration;
using XData.Data.Element;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Services
{
    public class UsersService
    {
        protected ElementContext ElementContext = ConfigurationCreator.CreateElementContext();

        private Database _database = null;
        protected Database Database
        {
            get
            {
                if (_database == null)
                {
                    _database = ElementContext.Database;
                }
                return _database;
            }
        }

        private ElementQuerier _elementQuerier = null;
        protected ElementQuerier ElementQuerier
        {
            get
            {
                if (_elementQuerier == null)
                {
                    _elementQuerier = new ElementQuerier(ElementContext);
                }
                return _elementQuerier;
            }
        }

        public void Create(int employeeId, string userName, bool isDisabled)
        {
            XElement databaseSchema = ElementContext.SchemaManager.PrimarySchemaObject.DatabaseSchema;
            string sql = "SELECT * FROM InitialPassword";
            IEnumerable<XElement> xInitials = Database.SqlQuery(databaseSchema, "InitialPassword", sql);
            int count = xInitials.Count();

            string password;
            string format;
            string algorithm;
            string salt;
            if (count == 0)
            {
                XElement elementSchema = databaseSchema.GetElementSchema("InitialPassword");
                XElement fieldSchema = elementSchema.Element("Password");
                XElement defaultSchema = fieldSchema.Element("DefaultValue");
                password = defaultSchema.Element("Value").Value;

                fieldSchema = elementSchema.Element("PasswordFormat");
                defaultSchema = fieldSchema.Element("DefaultValue");
                format = defaultSchema.Element("Value").Value;

                fieldSchema = elementSchema.Element("PasswordAlgorithm");
                defaultSchema = fieldSchema.Element("DefaultValue");
                algorithm = (defaultSchema == null) ? string.Empty : defaultSchema.Element("Value").Value;

                fieldSchema = elementSchema.Element("PasswordSalt");
                defaultSchema = fieldSchema.Element("DefaultValue");
                salt = (defaultSchema == null) ? string.Empty : defaultSchema.Element("Value").Value;
            }
            else
            {
                XElement xInitialPassword = xInitials.Elements().First();
                password = xInitialPassword.Element("Password").Value;
                format = xInitialPassword.Element("PasswordFormat").Value;
                algorithm = xInitialPassword.Element("PasswordAlgorithm").Value;
                salt = xInitialPassword.Element("PasswordSalt").Value;
            }

            if (Database is SqlDatabase)
            {
                sql = "SELECT MAX(Id) Id FROM Users";
                IEnumerable<XElement> xIds = Database.SqlQuery("User", sql);
                int id = int.Parse(xIds.First().Element("Id").Value) + 1;

                algorithm = string.IsNullOrWhiteSpace(algorithm) ? "null" : algorithm;
                salt = string.IsNullOrWhiteSpace(salt) ? "null" : salt;

                sql = @"
 INSERT INTO Users
 (Id, EmployeeId, UserName, LoweredUserName, Password, PasswordFormat, PasswordAlgorithm, PasswordSalt, IsDisabled)
 VALUES
 ({0}, {1}, @UserName, @LoweredUserName, '{2}', {3}, {4}, {5}, @IsDisabled)";
                sql = string.Format(sql, id, employeeId, password, format, algorithm, salt);

                DbParameter[] parameters = new DbParameter[2];
                parameters[0] = Database.CreateDbParameter("@UserName", userName);
                parameters[1] = Database.CreateDbParameter("@LoweredUserName", userName.ToLower());
                parameters[2] = Database.CreateDbParameter("@IsDisabled", isDisabled);

                int i = Database.ExecuteSqlCommand(sql, parameters);
                return;
            }

            string typeName = Database.GetType().FullName;
            if (typeName == "XData.Data.Objects.OracleDatabase")
            {
                sql = "SELECT MAX(Id) Id FROM Users";
                IEnumerable<XElement> xIds = Database.SqlQuery("User", sql);
                int id = int.Parse(xIds.First().Element("Id").Value) + 1;

                sql = @"
 INSERT INTO Users
 (Id, EmployeeId, UserName, LoweredUserName, Password, PasswordFormat, PasswordAlgorithm, PasswordSalt, IsDisabled)
 VALUES
 (:Id, :EmployeeId, :UserName, :LoweredUserName, :Password, :Format, :Algorithm, :Salt, :IsDisabled)";

                DbParameter[] parameters = new DbParameter[8];
                parameters[0] = Database.CreateDbParameter(":Id", id);
                parameters[1] = Database.CreateDbParameter(":EmployeeId", employeeId);
                parameters[2] = Database.CreateDbParameter(":UserName", userName);
                parameters[3] = Database.CreateDbParameter(":LoweredUserName", userName.ToLower());
                parameters[4] = Database.CreateDbParameter(":Password", password);
                parameters[5] = Database.CreateDbParameter(":Format", int.Parse(format));
                parameters[6] = Database.CreateDbParameter(":IsDisabled", isDisabled ? 1 : 0);

                if (string.IsNullOrWhiteSpace(algorithm))
                {
                    parameters[6] = Database.CreateDbParameter(":Algorithm", DBNull.Value);
                }
                else
                {
                    parameters[6] = Database.CreateDbParameter(":Algorithm", Guid.Parse(algorithm));
                }
                if (string.IsNullOrWhiteSpace(salt))
                {
                    parameters[7] = Database.CreateDbParameter(":Salt", DBNull.Value);
                }
                else
                {
                    parameters[7] = Database.CreateDbParameter(":Salt", salt);
                }

                int i = Database.ExecuteSqlCommand(sql, parameters);
                return;
            }

            throw new NotSupportedException(typeName);
        }

        public void SetPassword(int id, string newPassword)
        {
            AccountService.SetPassword(id, newPassword, Database);
        }

        public bool ValidatePassword(string password, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "The new password is required and cannot be empty.";
                return false;
            }
            if (password.Length < 6 || password.Length > 20)
            {
                errorMessage = "The password must be at least 6 and not more than 20 characters long.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public void SetUserName(int id, string newUserName)
        {
            if (Database is SqlDatabase)
            {
                string sql = string.Format("UPDATE Users SET UserName = @UserName, LoweredUserName = @LoweredUserName WHERE Id = {0}", id);
                DbParameter parameter1 = Database.CreateDbParameter("@UserName", newUserName);
                DbParameter parameter2 = Database.CreateDbParameter("@LoweredUserName", newUserName.ToLower());
                int i = Database.ExecuteSqlCommand(sql, parameter1, parameter2);
                return;
            }

            string typeName = Database.GetType().FullName;
            if (typeName == "XData.Data.Objects.OracleDatabase")
            {
                string sql = string.Format("UPDATE Users SET UserName = :UserName, LoweredUserName = :LoweredUserName WHERE Id = {0}", id);
                DbParameter parameter1 = Database.CreateDbParameter(":UserName", newUserName);
                DbParameter parameter2 = Database.CreateDbParameter(":LoweredUserName", newUserName.ToLower());
                int i = Database.ExecuteSqlCommand(sql, parameter1, parameter2);
                return;
            }

            throw new NotSupportedException(typeName);
        }

        public void Enable(int id)
        {
            string sql = string.Format("UPDATE Users SET IsDisabled = 0 WHERE Id = {0} AND IsDisabled = 1", id);
            int i = Database.ExecuteSqlCommand(sql);
        }

        public void Disable(int id)
        {
            string sql = string.Format("UPDATE Users SET IsDisabled = 1 WHERE Id = {0} AND IsDisabled = 0", id);
            int i = Database.ExecuteSqlCommand(sql);
        }

        public void Unlock(int id)
        {
            string sql = string.Format("UPDATE Users SET IsLockedOut = 0, FailedPasswordAttemptCount = 0 WHERE Id = {0} AND IsLockedOut = 1", id);
            int i = Database.ExecuteSqlCommand(sql);
        }

        public void GrantRoles(int id, string roleIds)
        {
            XElement element = new XElement("User");
            element.SetElementValue("Id", id);
            XElement xRoles = new XElement("Roles");
            element.Add(xRoles);

            if (!string.IsNullOrWhiteSpace(roleIds))
            {
                string[] roles = roleIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string role in roles)
                {
                    XElement xRole = new XElement("Role");
                    xRole.SetElementValue("Id", role);
                    xRoles.Add(xRole);
                }
            }

            ElementContext.Update(element);
        }

        public void SetInitialPassword(string initialPassword)
        {
            int format;
            Guid algorithm;
            string salt;
            string encrypted = AccountService.Encrypt(initialPassword, out format, out algorithm, out salt);

            string sql = "SELECT * FROM InitialPassword";
            IEnumerable<XElement> result = Database.SqlQuery(ElementContext.SchemaManager.PrimarySchemaObject.DatabaseSchema, "InitialPassword", sql);
            int count = result.Count();

            if (Database is SqlDatabase)
            {
                if (count == 0)
                {
                    sql = "INSERT INTO InitialPassword (Password, PasswordFormat, PasswordAlgorithm, PasswordSalt) VALUES (@Password, @Format, @Algorithm, @Salt)";
                }
                else
                {
                    sql = "UPDATE InitialPassword SET Password = @Password, PasswordFormat = @Format, PasswordAlgorithm = @Algorithm, PasswordSalt = @Salt";
                }
                DbParameter[] parameters = new DbParameter[4];
                parameters[0] = Database.CreateDbParameter("@Password", encrypted);
                parameters[1] = Database.CreateDbParameter("@Format", format);
                if (algorithm == null)
                {
                    parameters[2] = Database.CreateDbParameter("@Algorithm", DBNull.Value);
                }
                else
                {
                    parameters[2] = Database.CreateDbParameter("@Algorithm", algorithm);
                }
                if (string.IsNullOrWhiteSpace(salt))
                {
                    parameters[3] = Database.CreateDbParameter("@Salt", DBNull.Value);
                }
                else
                {
                    parameters[3] = Database.CreateDbParameter("@Salt", salt);
                }

                int i = Database.ExecuteSqlCommand(sql, parameters);
                return;
            }

            string typeName = Database.GetType().FullName;
            if (typeName == "XData.Data.Objects.OracleDatabase")
            {
                if (count == 0)
                {
                    sql = "INSERT INTO InitialPassword (Password, PasswordFormat, PasswordAlgorithm, PasswordSalt) VALUES (:Password, :Format, :Algorithm, :Salt)";
                }
                else
                {
                    sql = "UPDATE InitialPassword SET Password = :Password, PasswordFormat = :Format, PasswordAlgorithm = :Algorithm, PasswordSalt = :Salt";
                }
                DbParameter[] parameters = new DbParameter[4];
                parameters[0] = Database.CreateDbParameter(":Password", encrypted);
                parameters[1] = Database.CreateDbParameter(":Format", format);
                if (algorithm == null)
                {
                    parameters[2] = Database.CreateDbParameter(":Algorithm", DBNull.Value);
                }
                else
                {
                    parameters[2] = Database.CreateDbParameter(":Algorithm", algorithm);
                }
                if (string.IsNullOrWhiteSpace(salt))
                {
                    parameters[3] = Database.CreateDbParameter(":Salt", DBNull.Value);
                }
                else
                {
                    parameters[3] = Database.CreateDbParameter(":Salt", salt);
                }

                int i = Database.ExecuteSqlCommand(sql, parameters);
                return;
            }

            throw new NotSupportedException(typeName);
        }

        public bool IsUniqueUserNameByReferrer(string userName, string referrer)
        {
            bool isUnique = false;

            // .../Admin/Users/SetUserName/1 // .../Admin/Users/Create
            string[] ss = referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss[ss.Length - 2] == "SetUserName" && ss[ss.Length - 3] == "Users" && ss[ss.Length - 4] == "Admin")
            {
                string id = ss[ss.Length - 1];
                isUnique = IsUniqueUserName(userName, id);               
            }
            else if (ss[ss.Length - 1] == "Create" && ss[ss.Length - 2] == "Users" && ss[ss.Length - 3] == "Admin")
            {
                isUnique = IsUniqueUserName(userName);
            }

            return isUnique;
        }

        public bool IsUniqueUserName(string userName)
        {
            return ElementQuerier.IsUnique("User", string.Format("LoweredUserName eq '{0}'", userName.ToLower()), ElementContext.PrimarySchema);
        }

        public bool IsUniqueUserName(string userName, string id)
        {
            return ElementQuerier.IsUnique("User", string.Format("LoweredUserName eq '{0}'", userName.ToLower()), id, ElementContext.PrimarySchema);
        }

        public void Update(XElement element)
        {
            ElementContext.Update(element);
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
@"SELECT Id, EmployeeId, UserName, IsDisabled, IsLockedOut FROM Users WHERE LoweredUserName = @LoweredUserName";

                parameter = Database.CreateDbParameter("@LoweredUserName", userName);
                return sql;
            }

            string typeName = Database.GetType().FullName;
            if (typeName == "XData.Data.Objects.OracleDatabase")
            {
                string sql =
@"SELECT Id, EmployeeId, UserName, IsDisabled, IsLockedOut FROM Users WHERE LoweredUserName = :LoweredUserName";

                parameter = Database.CreateDbParameter(":LoweredUserName", userName.ToLower());
                return sql;
            }

            throw new NotSupportedException(typeName);
        }


    }
}
