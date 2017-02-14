using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using XData.Web.Mvc.Admin.Models;

namespace XData.Web.Mvc.Admin.Controllers
{
    [RoutePrefix("Admin/User")]
    public class EmployeeUserController : ApiController
    {
        [Route()]
        public HttpResponseMessage Post([FromBody]object value)
        {
            return new EmployeeUserModel().Create(value, this);
        }

        [Route()]
        public HttpResponseMessage Put([FromBody]object value)
        {
            return new EmployeeUserModel().Update(value, this);
        }
    }
}
