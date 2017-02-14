using System.Web.Http;
using System.Xml.Linq;
using XData.Data.Services;
using XData.Web.Http.Filters;

namespace XData.Web.Http.Controllers.Schemas
{
    [RoutePrefix("Schemas/Schema")]
    [HttpActionFilter]
    public class SchemaController : ApiController
    {
        protected SchemasService SchemasService = new SchemasService();

        [Route()]
        public XElement Get()
        {
            string prefix = Request.RequestUri.Scheme + "://" + Request.RequestUri.Host;
            prefix += (Request.RequestUri.Port == 80) ? string.Empty : ":" + Request.RequestUri.Port;
            prefix += "/";
            XElement index = new XElement("Index");

            XElement primaryIndex = new XElement("Schema", prefix + "Schemas/Primary/Schema");
            XElement primaryWarnings = SchemasService.GetWarnings();
            if (primaryWarnings.HasElements)
            {
                primaryIndex.Add(new XElement("Warning", prefix + "Schemas/Primary/Warning/"));
            }
            index.Add(primaryIndex);
            foreach (XElement schema in SchemasService.GetNamedSchemas())
            {
                string name = schema.Attribute("Name").Value;
                XElement schemaIndex = new XElement("Schema." + name, prefix + "Schemas/Schema/" + name);
                XElement warnings = SchemasService.GetWarnings(name);
                if (warnings.HasElements)
                {
                    schemaIndex.Add(new XElement("Warning", prefix + "Schemas/Warning/" + name));
                }
                index.Add(schemaIndex);
            }

            return index;
        }

        [Route("{name}")]
        public XElement Get(string name)
        {
            return SchemasService.GetSchema(name);
        }


    }
}
