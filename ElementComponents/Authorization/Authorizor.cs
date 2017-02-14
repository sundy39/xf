using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class Authorizor
    {
        protected ElementContext ElementContext;
        protected AuthorizationConfigGetter AuthorizationConfigGetter;

        public Authorizor(ElementContext elementContext, AuthorizationConfigGetter authorizationConfigGetter)
        {
            ElementContext = elementContext;
            AuthorizationConfigGetter = authorizationConfigGetter;
        }

        public virtual bool IsAuthorized(string route, string verb, bool isAuthorized, string user, IEnumerable<string> roles)
        {
            XElement config = AuthorizationConfigGetter.Get(route);
            if (config == null) return isAuthorized;

            foreach (XElement item in config.Elements())
            {
                XAttribute attr = item.Attribute("verbs");
                if (attr != null)
                {
                    string verbs = attr.Value.ToUpper();
                    if (!verbs.Contains(verb.ToUpper())) continue;
                }

                if (item.Name.LocalName == "allow")
                {
                    attr = item.Attribute("users");
                    if (attr != null)
                    {
                        string users = attr.Value.ToLower();
                        if (users == "?")
                        {
                            //invalid;
                        }
                        else if (users == "*")
                        {
                            return true;
                        }
                        else if (isAuthorized && users.Contains(user.ToLower()))
                        {
                            return true;
                        }
                        continue;
                    }

                    attr = item.Attribute("roles");
                    if (attr != null)
                    {
                        string strRoles = attr.Value.ToLower();
                        foreach (string role in roles)
                        {
                            if (strRoles.Contains(role.ToLower())) return true;
                        }
                    }
                }
                else if (item.Name.LocalName == "deny")
                {
                    attr = item.Attribute("users");
                    if (attr != null)
                    {
                        string users = attr.Value.ToLower();
                        if (users == "?")
                        {
                            if (!isAuthorized) return false;
                        }
                        else if (users == "*")
                        {
                            return false;
                        }
                        else if (isAuthorized && users.Contains(user.ToLower()))
                        {
                            return false;
                        }
                        continue;
                    }

                    attr = item.Attribute("roles");
                    if (attr != null)
                    {
                        string strRoles = attr.Value.ToLower();
                        foreach (string role in roles)
                        {
                            if (strRoles.Contains(role.ToLower())) return false;
                        }
                    }
                }
            }

            return isAuthorized;
        }


    }
}
