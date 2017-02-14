using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Configuration;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class CurrentUser
    {       
        protected ElementContext ElementContext;
        protected ElementQuerier ElementQuerier;
        protected ICurrentUserIdentityGetter CurrentUserIdentityGetter;

        internal protected CurrentUser(ElementContext elementContext, ICurrentUserIdentityGetter currentUserIdentityGetter)
        {
            ElementContext = elementContext;
            CurrentUserIdentityGetter = currentUserIdentityGetter;
            ElementQuerier = new ElementQuerier(ElementContext);
        }

        public XElement ToElement()
        {
            KeyValuePair<string, string> pair = CurrentUserIdentityGetter.Get();
            string key = pair.Key;
            string value = pair.Value;
            if (value == null) return null;

            string filter = null;
            if (key == "UserName" || key == "Email" || key == "Mobile")
            {
                filter = string.Format("{0} eq '{1}'", key, value);
            }
            else if (key == "Id")
            {
                filter = string.Format("{0} eq {1}", key, value);
            }

            if (filter == null) throw new ArgumentException(key);

            XElement result = ElementQuerier.GetSet("User", null, filter, null);
            return result.Elements().First();
        }
    }

    internal class CurrentUserFactory
    {
        public CurrentUser Create(ElementContext elementContext)
        {
            ICurrentUserIdentityGetter currentUserIdentityGetter = new CurrentUserIdentityGetterFactory().Create();
            return new CurrentUser(elementContext, currentUserIdentityGetter);
        }
    }

}
