using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using XData.Data.Services;

namespace XData.WebApp.Controllers.Admin
{
    [RoutePrefix("Admin/Employees")]
    [Route("{action=Index}")]
    public class EmployeesController : Controller
    {
        // GET: Employees
        public ActionResult Index()
        {
            return View();
        }

        [Route("IsUniqueUserName")]
        public ActionResult IsUniqueUserName()
        {
            bool isUnique = false;

            string id = this.Request.QueryString["Id"];
            string userName = this.Request.QueryString["UserName"];

            UsersService usersService = new UsersService();
            if (string.IsNullOrWhiteSpace(id))
            {
                isUnique = usersService.IsUniqueUserName(userName);
            }
            else
            {
                isUnique = usersService.IsUniqueUserName(userName, id);
            }
            var obj = new { valid = isUnique };
            return Json(obj, JsonRequestBehavior.AllowGet);
        }


    }
}
