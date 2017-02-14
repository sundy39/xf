﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace XData.Web.Mvc.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {   
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Privacy()
        {
            ViewBag.Message = "Privacy Statement";

            return View();
        }

        public ActionResult Terms()
        {
            ViewBag.Message = "Terms of Use";

            return View();
        }


    }
}
