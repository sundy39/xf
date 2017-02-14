using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using XData.Data.Services;
using XData.Web.Http.Filters;

namespace XData.Web.Http.Controllers
{
    [RoutePrefix("Data")]
    [HttpActionFilter]
    public class DataController : ApiController
    {
        protected DataService DataService = new DataService();

        protected IEnumerable<KeyValuePair<string, string>> GetNameValuePairs()
        {
            IEnumerable<KeyValuePair<string, string>> nameValuePairs = Request.GetQueryNameValuePairs();

            if (System.Web.HttpContext.Current.Request.UrlReferrer == null) return nameValuePairs;
            string referrer = System.Web.HttpContext.Current.Request.UrlReferrer.AbsolutePath;
            if (string.IsNullOrWhiteSpace(referrer)) return nameValuePairs;
            
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>(nameValuePairs);
            if (list.Any(p => p.Key == "referrer"))
            {
                list.Remove(list.First(p => p.Key == "referrer"));
            }

            list.Add(new KeyValuePair<string, string>("referrer", referrer));
            return list;
        }

        [Route()]
        public XElement Get()
        {
            return DataService.Get(GetNameValuePairs(), Request.GetAccept());
        }

        [Route("Count")]
        public XElement GetCount()
        {
            return DataService.GetCount(GetNameValuePairs(), Request.GetAccept());
        }

        [Route()]
        public XElement Post([FromBody]object value)
        {
            return DataService.Create(value, GetNameValuePairs());
        }

        [Route("Xml")]
        public XElement Post([FromBody]XElement value)
        {
            return DataService.Create(value, GetNameValuePairs());
        }

        [Route()]
        public XElement Put([FromBody]object value)
        {
            return DataService.Update(value, GetNameValuePairs());
        }

        [Route("Xml")]
        public XElement Put([FromBody]XElement value)
        {
            return DataService.Update(value, GetNameValuePairs());
        }

        [Route()]
        public XElement Delete()
        {
            return DataService.Delete(GetNameValuePairs());
        }

        [HttpDelete]
        [Route("Batch")]
        public XElement Delete([FromBody]object value)
        {
            return DataService.Delete(value, GetNameValuePairs());
        }

        [HttpDelete]
        [Route("Xml/Batch")]
        public XElement Delete([FromBody]XElement value)
        {
            return DataService.Delete(value, GetNameValuePairs());
        }


    }
}
