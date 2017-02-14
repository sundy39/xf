using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using XData.Data.Services;

namespace XData.Web.Mvc.Admin.Controllers
{
    [RoutePrefix("Admin/Roles")]
    [Route("{action=Index}")]
    public class RolesController : Controller
    {
        protected RolesService RolesService = new RolesService();

        public ActionResult Index()
        {
            return View();
        }

        [Route("Details/{id}")]
        public ActionResult Details(int id)
        {
            return View();
        }

        [HttpGet]
        [Route("Create")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public ActionResult Edit(int id)
        {
            return View();
        }

        [Route("Delete/{id}")]
        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpPost]
        [Route("Delete")]
        public ActionResult Delete(FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        [Route("IsUniqueRoleName")]
        public ActionResult IsUniqueRoleName()
        {
            bool isUnique = false;
            string roleName = this.Request.QueryString["RoleName"];

            // .../Admin/Roles/Edit/1 // .../Admin/Roles/Create
            string referrer = Request.UrlReferrer.AbsolutePath;

            isUnique = RolesService.IsUniqueRoleName(roleName, referrer);
            var obj = new { valid = isUnique };
            return Json(obj, JsonRequestBehavior.AllowGet);
        }


    }
}
