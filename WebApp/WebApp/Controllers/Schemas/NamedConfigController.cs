using System.Web.Http;
using System.Xml.Linq;
using XData.Data.Services;
using XData.Web.Http.Filters;

namespace XData.Web.Http.Controllers.Schemas
{
    [RoutePrefix("Schemas/NamedConfig")]
    [HttpActionFilter]
    public class NamedConfigController : ApiController
    {
        protected SchemasService SchemasService = new SchemasService();

        [Route()]
        public XElement Get()
        {
            string prefix = Request.RequestUri.Scheme + "://" + Request.RequestUri.Host;
            prefix += (Request.RequestUri.Port == 80) ? string.Empty : ":" + Request.RequestUri.Port;
            prefix += "/";
            XElement index = new XElement("Index");

            foreach (XElement schema in SchemasService.GetNamedConfigs())
            {
                string name = schema.Attribute("Name").Value;
                index.Add(new XElement("Config." + name, prefix + "Schemas/NamedConfig/" + name));
            }

            return index;
        }

        [Route("{name}")]
        public XElement Get(string name)
        {
            return SchemasService.GetNamedConfig(name);
        }


    }
}
