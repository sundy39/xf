using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Configuration;
using XData.Data.Element;

namespace XData.Data.Services
{
    public class RolesService
    {
        protected ElementContext ElementContext = ConfigurationCreator.CreateElementContext();

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

        public bool IsUniqueRoleName(string roleName, string referrer)
        {
            bool isUnique = false;

            // .../Admin/Roles/Edit/1 // .../Admin/Roles/Create          
            string[] ss = referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss[ss.Length - 2] == "Edit" && ss[ss.Length - 3] == "Roles" && ss[ss.Length - 4] == "Admin")
            {
                string id = ss[ss.Length - 1];
                isUnique = ElementQuerier.IsUnique("Role", string.Format("LoweredRoleName eq '{0}'", roleName.ToLower()), id, ElementContext.PrimarySchema);
            }
            else if (ss[ss.Length - 1] == "Create" && ss[ss.Length - 2] == "Roles" && ss[ss.Length - 3] == "Admin")
            {
                isUnique = ElementQuerier.IsUnique("Role", string.Format("LoweredRoleName eq '{0}'", roleName.ToLower()), ElementContext.PrimarySchema);
            }

            return isUnique;
        }

        public IEnumerable<string> GetRoles(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return new string[0];

            XElement result = ElementQuerier.GetSet("UserRole", "RoleName", string.Format("LoweredUserName eq '{0}'", userName.ToLower()), null);
            if (!result.HasElements) return new string[0];
            return result.Elements().Select(x => x.Element("RoleName").Value).ToArray();
        }


    }
}
