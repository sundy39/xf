using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Web;

namespace XData.Data.Components
{
    public class CurrentUserIdentityGetter : ICurrentUserIdentityGetter
    {
        public KeyValuePair<string, string> Get()
        {
            if (Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                string userName = Thread.CurrentPrincipal.Identity.Name;
                return new KeyValuePair<string, string>("UserName", userName);
            }

            //if (HttpContext.Current.User.Identity.IsAuthenticated)
            //{
            //    string username = HttpContext.Current.User.Identity.Name;
            //    return new KeyValuePair<string, string>("Username", username);
            //}

            return new KeyValuePair<string, string>("UserName", null);
        }
    }
}
