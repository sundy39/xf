using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using XData.WebApp.Models;

namespace XData.Web.Http.Controllers
{
    [RoutePrefix("User")]
    public class UserController : ApiController
    {
        [AllowAnonymous]
        [Route()]
        public HttpResponseMessage Get()
        {
            return WebSecurity.GetApiVerificationToken();
        }

        // Login
        [AllowAnonymous]
        [Route()]
        public HttpResponseMessage Post([FromBody]XElement value)
        {
            WebSecurity.ValidateApiVerificationToken(this.Request, value);

            return WebSecurity.Login(value);
        }

        // ChangePassword
        [Route()]
        public HttpResponseMessage Put([FromBody]XElement value)
        {
            return WebSecurity.ChangePassword(value, this);
        }

        // Logout
        [Route()]
        public void Delete([FromBody]XElement value)
        {
            WebSecurity.ValidateApiVerificationToken(this.Request, value);

            WebSecurity.Logout();
        }


    }
}
