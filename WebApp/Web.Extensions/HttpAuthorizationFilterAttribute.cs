using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http;
using System.Threading;
using System.Net.Http;
using XData.Data.Services;

namespace XData.Web.Http.Filters
{
    public class HttpAuthorizationFilterAttribute : AuthorizeAttribute //AuthorizationFilterAttribute
    {
        protected AccountService AccountService = new AccountService();

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            bool isAuthorized = base.IsAuthorized(actionContext);
            string userName = null;
            if (Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                userName = Thread.CurrentPrincipal.Identity.Name;
            }
            string verb = actionContext.Request.Method.ToString();            
            return AccountService.IsAuthorized(actionContext.Request.RequestUri.AbsolutePath, verb, isAuthorized, userName);
        }
    }
}
