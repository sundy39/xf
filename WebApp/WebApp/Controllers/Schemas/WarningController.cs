using System.Web.Http;
using System.Xml.Linq;
using XData.Data.Services;
using XData.Web.Http.Filters;

namespace XData.Web.Http.Controllers.Schemas
{
    [RoutePrefix("Schemas/Warning")]
    [HttpActionFilter]
    public class WarningController : ApiController
    {
        protected SchemasService SchemasService = new SchemasService();

        [Route()]
        public XElement Get()
        {
            string prefix = Request.RequestUri.Scheme + "://" + Request.RequestUri.Host;
            prefix += (Request.RequestUri.Port == 80) ? string.Empty : ":" + Request.RequestUri.Port;
            prefix += "/";
            XElement index = new XElement("Index");

            XElement primaryWarnings = SchemasService.GetWarnings();
            if (primaryWarnings.HasElements)
            {
                index.Add(new XElement("Warning", prefix + "Schemas/Primary/Warning/"));
            }
            foreach (XElement schema in SchemasService.GetNamedSchemas())
            {
                string name = schema.Attribute("Name").Value;              
                XElement warnings = SchemasService.GetWarnings(name);
                if (warnings.HasElements)
                {
                    index.Add(new XElement("Warning." + name, prefix + "Schemas/Warning/" + name));
                } 
            }

            return index;
        }

        [Route("{name}")]
        public XElement Get(string name)
        {
            return SchemasService.GetWarnings(name);
        }


    }
}
