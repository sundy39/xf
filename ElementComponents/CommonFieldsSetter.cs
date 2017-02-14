using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XData.Data.Components
{
    public class CommonFieldsSetter
    {
        public const string USER_ID_FIELD_NAME = "Id";
        public const string USER_NAME_FIELD_NAME = "UserName";

        public const string CREATED_AT_FIELD_NAME = "CreatedAt";
        public const string CREATED_USERID_FIELD_NAME = "CreatedUserId";
        public const string CREATED_USERNAME_FIELD_NAME = "CreatedUserName";

        public const string UPDATED_AT_FIELD_NAME = "UpdatedAt";
        public const string UPDATED_USERID_FIELD_NAME = "UpdatedUserId";       
        public const string UPDATED_USERNAME_FIELD_NAME = "UpdatedUserName";

        private string _userIdFieldName = USER_ID_FIELD_NAME;
        public string UserIdFieldName
        {
            get { return _userIdFieldName; }
            set { _userIdFieldName = value; }
        }

        private string _userNameFieldName = USER_NAME_FIELD_NAME;
        public string UserNameFieldName
        {
            get { return _userNameFieldName; }
            set { _userNameFieldName = value; }
        }

        private string _createdDateFieldName = CREATED_AT_FIELD_NAME;
        public string CreatedDateFieldName
        {
            get { return _createdDateFieldName; }
            set { _createdDateFieldName = value; }
        }

        private string _createdUserIdFieldName = CREATED_USERID_FIELD_NAME;
        public string CreatedUserIdFieldName
        {
            get { return _createdUserIdFieldName; }
            set { _createdUserIdFieldName = value; }
        }

        private string _createdUserNameFieldName = CREATED_USERNAME_FIELD_NAME;
        public string CreatedUserNameFieldName
        {
            get { return _createdUserNameFieldName; }
            set { _createdUserNameFieldName = value; }
        }

        private string _updatedDateFieldName = UPDATED_AT_FIELD_NAME;
        public string UpdatedDateFieldName
        {
            get { return _updatedDateFieldName; }
            set { _updatedDateFieldName = value; }
        }

        private string _updatedUserIdFieldName = UPDATED_USERID_FIELD_NAME;
        public string UpdatedUserIdFieldName
        {
            get { return _updatedUserIdFieldName; }
            set { _updatedUserIdFieldName = value; }
        }

        private string _updatedUserNameFieldName = UPDATED_USERNAME_FIELD_NAME;
        public string UpdatedUserNameFieldName
        {
            get { return _updatedUserNameFieldName; }
            set { _updatedUserNameFieldName = value; }
        }

        public virtual void SetOnInserting(XElement node, XElement user)
        {
            if (user == null) return;

            XElement element = node.Element(UpdatedDateFieldName);
            if (element != null) element.Remove();
            element = node.Element(UpdatedUserIdFieldName);
            if (element != null) element.Remove();
            element = node.Element(UpdatedUserNameFieldName);
            if (element != null) element.Remove();

            string now = DateTime.Now.ToCanonicalString();
            string userId = user.Element(UserIdFieldName).Value;
            string usersName = user.Element(UserNameFieldName).Value;

            node.SetElementValue(CreatedDateFieldName, DateTime.Now);
            node.SetElementValue(CreatedUserIdFieldName, userId);
            node.SetElementValue(CreatedUserNameFieldName, usersName);
        }

        public virtual void SetOnUpdating(XElement node, XElement user)
        {
            if (user == null) return;

            XElement element = node.Element(CreatedDateFieldName);
            if (element != null) element.Remove();
            element = node.Element(CreatedUserIdFieldName);
            if (element != null) element.Remove();
            element = node.Element(CreatedUserNameFieldName);
            if (element != null) element.Remove();

            string now = DateTime.Now.ToCanonicalString();
            string userId = user.Element(UserIdFieldName).Value;
            string usersName = user.Element(UserNameFieldName).Value;

            node.SetElementValue(UpdatedDateFieldName, DateTime.Now);
            node.SetElementValue(UpdatedUserIdFieldName, userId);
            node.SetElementValue(UpdatedUserNameFieldName, usersName);
        }


    }
}