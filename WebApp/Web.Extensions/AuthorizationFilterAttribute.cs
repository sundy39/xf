using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using XData.Data.Services;

namespace XData.Web.Mvc
{
    public class AuthorizationFilterAttribute : AuthorizeAttribute
    {
        protected AccountService AccountService = new AccountService();

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            bool isAuthorized = base.AuthorizeCore(httpContext);
            string userName = null;
            if (Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                userName = Thread.CurrentPrincipal.Identity.Name;
            }
            return AccountService.IsAuthorized(httpContext.Request.Url.AbsolutePath, httpContext.Request.HttpMethod, isAuthorized, userName);
        }


    }
}
