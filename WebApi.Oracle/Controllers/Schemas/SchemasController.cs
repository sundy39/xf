using System.Web.Http;
using System.Xml.Linq;
using XData.Data.Services;
using XData.Web.Http.Filters;

namespace XData.Web.Http.Controllers.Schemas
{
    [RoutePrefix("Schemas")]
    [HttpActionFilter]
    public class SchemasController : ApiController
    {
        protected SchemasService SchemasService = new SchemasService();

        [Route()]
        public XElement Get()
        {
            string prefix = Request.RequestUri.Scheme + "://" + Request.RequestUri.Host;
            prefix += (Request.RequestUri.IsDefaultPort) ? string.Empty : ":" + Request.RequestUri.Port;
            prefix += "/";
            XElement index = new XElement("Index");

            // Primary
            index.Add(new XElement("NameMapConfig", prefix + "Schemas/Primary/NameMap"));
            index.Add(new XElement("DatabaseSchema", prefix + "Schemas/Primary/Database"));
            index.Add(new XElement("Config", prefix + "Schemas/Primary/Config"));
            XElement primarySchema = new XElement("Schema", prefix + "Schemas/Primary/Schema");
            XElement primaryWarnings = SchemasService.GetWarnings();
            if (primaryWarnings.HasElements)
            {
                primarySchema.Add(new XElement("Warning", prefix + "Schemas/Primary/Warning/"));
            }
            index.Add(primarySchema);
            index.Add(new XElement("Publish", prefix + "Schemas/Primary/Publish"));

            // Named
            foreach (XElement schema in SchemasService.GetNamedConfigs())
            {
                string name = schema.Attribute("Name").Value;
                XElement namedIndex = new XElement("Named." + name);
                namedIndex.Add(new XElement("Config", prefix + "Schemas/NamedConfig/" + name));

                XElement namedSchema = new XElement("Schema", prefix + "Schemas/Schema/" + name);
                XElement namedWarnings = SchemasService.GetWarnings(name);
                if (namedWarnings.HasElements)
                {
                    namedSchema.Add(new XElement("Warning", prefix + "Schemas/Warning/" + name));
                }
                namedIndex.Add(namedSchema); 
                namedIndex.Add(new XElement("Publish", prefix + "Schemas/Publish/" + name));
                index.Add(namedIndex);               
            }
            return index;
        }


    }
}
