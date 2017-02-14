using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using XData.Data.Services;
using XData.WebApp.Models;

namespace XData.Web.Mvc.Admin.Controllers
{
    [RoutePrefix("Admin/Users")]
    [Route("{action=Index}")]
    public class UsersController : Controller
    {
        protected UsersService UsersService = new UsersService();

        public ActionResult Index()
        {
            return View();
        }

        [Route("Create")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Route("Create")]
        public ActionResult Create(CreateUserModel model)
        {
            if (ModelState.IsValid)
            {
                UsersService.Create(model.EmployeeId, model.UserName, model.IsDisabled);
                return Redirect("/Admin/Users/Index" + GetQueryString());
            }

            return View(model);
        }

        [Route("Delete/{id}")]
        public ActionResult Delete(int id)
        {
            return View();
        }

        [Route("Details/{id}")]
        public ActionResult Details(int id)
        {
            return View();
        }

        [Route("SetUserName/{id}")]
        public ActionResult SetUserName(int id)
        {
            return View();
        }

        [HttpPost]
        [Route("SetUserName/{id}")]
        public ActionResult SetUserName(int id, SetUserNameModel model)
        {
            if (ModelState.IsValid)
            {
                UsersService.SetUserName(id, model.NewUserName);
                return Redirect("/Admin/Users/Index" + GetQueryString());
            }

            return View(model);
        }

        [Route("SetPassword/{id}")]
        public ActionResult SetPassword(int id)
        {
            return View();
        }

        [HttpPost]
        [Route("SetPassword/{id}")]
        public ActionResult SetPassword(int id, FormCollection collection)
        {
            string newPassword = collection["NewPassword"];

            string errorMessage;
            if (UsersService.ValidatePassword(newPassword, out errorMessage))
            {
                UsersService.SetPassword(id, newPassword);
                return Redirect("/Admin/Users/Index" + GetQueryString());
            }
            ModelState.AddModelError("NewPassword", errorMessage);

            return View();
        }

        [Route("Enable/{id}")]
        public ActionResult Enable(int id)
        {
            return View();
        }

        [HttpPost]
        [Route("Enable/{id}")]
        public ActionResult Enable(int id, FormCollection collection)
        {
            UsersService.Enable(id);
            return Redirect("/Admin/Users/Index" + GetQueryString());
        }

        [Route("Disable/{id}")]
        public ActionResult Disable(int id)
        {
            return View();
        }

        [HttpPost]
        [Route("Disable/{id}")]
        public ActionResult Disable(int id, FormCollection collection)
        {
            UsersService.Disable(id);
            return Redirect("/Admin/Users/Index" + GetQueryString());
        }

        [Route("Unlock/{id}")]
        public ActionResult Unlock(int id)
        {
            return View();
        }

        [HttpPost]
        [Route("Unlock/{id}")]
        public ActionResult Unlock(int id, FormCollection collection)
        {
            UsersService.Unlock(id);
            return Redirect("/Admin/Users/Index" + GetQueryString());
        }

        [Route("GrantRoles/{id}")]
        public ActionResult GrantRoles(int id)
        {
            return View();
        }

        [HttpPost]
        [Route("GrantRoles/{id}")]
        public ActionResult GrantRoles(int id, FormCollection collection)
        {
            string roleIds = collection["RoleId"];
            UsersService.GrantRoles(id, roleIds);
            return Redirect("/Admin/Users/Index" + GetQueryString());
        }

        [Route("SetInitialPassword")]
        public ActionResult SetInitialPassword()
        {
            return View();
        }

        [HttpPost]
        [Route("SetInitialPassword")]
        public ActionResult SetInitialPassword(SetInitialPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                UsersService.SetInitialPassword(model.NewPassword);
                string redirectUrl = this.Request.Form["RedirectUrl"];
                return Redirect(redirectUrl);
            }

            return View(model);
        }

        [Route("IsUniqueUserName")]
        public ActionResult IsUniqueUserName()
        {
            bool isUnique = false;

            // .../Admin/Users/SetUserName/1 // .../Admin/Users/Create  
            string referrer = Request.UrlReferrer.AbsolutePath;
            string userName = (referrer.EndsWith("Create")) ? this.Request.QueryString["UserName"] : this.Request.QueryString["NewUserName"];

            isUnique = UsersService.IsUniqueUserNameByReferrer(userName, referrer);
            var obj = new { valid = isUnique };
            return Json(obj, JsonRequestBehavior.AllowGet);
        }

        protected string GetQueryString()
        {
            string queryString = this.Request.RawUrl;
            int index = queryString.IndexOf("?");
            if (index == -1) return string.Empty;
            return queryString.Substring(index);
        }


    }
}
