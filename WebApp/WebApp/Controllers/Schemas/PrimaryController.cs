using System;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;
using XData.Data.Services;
using XData.Web.Http.Filters;

namespace XData.Web.Http.Controllers.Schemas
{
    [RoutePrefix("Schemas/Primary")]
    [HttpActionFilter]
    public class PrimaryController : ApiController
    {
        protected SchemasService SchemasService = new SchemasService();

        [Route()]
        public XElement Get()
        {
            return SchemasService.GetPublishSchema();
        }

        //[Authorize(Roles = "Administer")]
        [Route("{property}")]
        public XElement Get(string property)
        {
            HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];
            HttpRequestBase request = context.Request;
            string filePath = (request.QueryString.Count == 0) ? null : request.QueryString[0];

            switch (property.ToLower())
            {
                case "schema":
                    return SchemasService.GetPrimarySchema();
                case "config":
                    return SchemasService.GetPrimaryConfig();
                case "namemap":
                    return SchemasService.GetNameMapConfig();
                case "database":
                    return SchemasService.GetDatabaseSchema();
                case "warning":
                    return SchemasService.GetWarnings();
                default:
                    return SchemasService.GetPublishSchema();
            }
        }


    }
}
