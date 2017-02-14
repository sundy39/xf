using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using XData.Data.Services;
using XData.Web.Http.Filters;

namespace XData.Web.Http.Controllers
{
    [RoutePrefix("XData")]
    [HttpActionFilter]
    public class XDataController : ApiController
    {
        protected XDataService XDataService = new XDataService();

        [Route()]
        [AllowAnonymous]
        public XElement Get()
        {
            XElement result = new XElement("URL", "http://xf.codeplex.com");
            result.SetAttributeValue("Accept", Request.GetAccept());
            return result;
        }

        [HttpGet]
        [Route("$now")]
        public XElement Now()
        {
            return XDataService.GetNow(Request.GetAccept());
        }

        [HttpGet]
        [Route("$utcnow")]
        public XElement UtcNow()
        {
            return XDataService.GetUtcNow(Request.GetAccept());
        }

        [HttpGet]
        [Route("{element}/$default")]
        public XElement Default(string element)
        {
            return XDataService.GetDefault(element, Request.GetQueryNameValuePairs(), Request.GetAccept());
        }

        [HttpGet]
        [Route("{set}/$count")]
        public XElement Count(string set)
        {
            return XDataService.GetCount(set, Request.GetQueryNameValuePairs(), Request.GetAccept());
        }

        [Route("{set}")]
        public XElement Get(string set)
        {
            if (set.IndexOf('(') == -1)
            {
                return XDataService.GetSet(set, Request.GetQueryNameValuePairs(), Request.GetAccept());
            }
            else
            {
                // XData/Users(1)
                return XDataService.Find(set, Request.GetQueryNameValuePairs(), Request.GetAccept());
            }
        }

        [HttpPost]
        [Route("{element}/$default")]
        public XElement PostDefault(string element, [FromBody]XElement config)
        {
            return XDataService.GetDefault(element, Request.GetQueryNameValuePairs(), Request.GetAccept(), config);
        }

        [HttpPost]
        [Route("{set}/$count")]
        public XElement PostCount(string set, [FromBody]XElement config)
        {
            return XDataService.GetCount(set, Request.GetQueryNameValuePairs(), Request.GetAccept(), config);
        }

        // {"key":""}, config is <key></Key>
        // {"key":null}, config is <key/>
        // {"key":undifined}, config is null 
        [HttpPost]
        [Route("{set}/config")]
        public XElement PostGet(string set, [FromBody]XElement config)
        {
            if (set.IndexOf('(') == -1)
            {
                return XDataService.GetSet(set, Request.GetQueryNameValuePairs(), Request.GetAccept(), config);
            }
            else
            {
                // XData/Users(1)
                return XDataService.Find(set, Request.GetQueryNameValuePairs(), Request.GetAccept(), config);
            }
        }

        [Route("")]
        public XElement Post([FromBody]XElement value)
        {
            if (value.Name.LocalName == "Units" && value.Elements().All(x => x.Attribute("Method") != null) ||
                value.Name.LocalName == "Unit" && value.Attribute("Method") != null)
            {
                // transcation packet
                return XDataService.Submit(value, Request.GetQueryNameValuePairs());
            }
            else
            {
                return XDataService.Create(value, Request.GetQueryNameValuePairs());
            }
        }

        // flat (non-hierarchical)
        [Route("")]
        public XElement Put([FromBody]XElement value)
        {
            return XDataService.Update(value, Request.GetQueryNameValuePairs());
        }

        // none-original
        [Route("")]
        public XElement Delete([FromBody]XElement value)
        {
            return XDataService.Delete(value, Request.GetQueryNameValuePairs());
        }

        [HttpPost]
        [Route("{set}")]
        public XElement PostJson(string set, [FromBody]object value)
        {
            if (set == "$trans")
            {
                // transcation packet
                return XDataService.Submit(value, Request.GetQueryNameValuePairs());
            }
            else
            {
                return XDataService.Create(set, value, Request.GetQueryNameValuePairs());
            }
        }

        [HttpPut]
        [Route("{set}")]
        public XElement PutJson(string set, [FromBody]object value)
        {
            return XDataService.Update(set, value, Request.GetQueryNameValuePairs());
        }

        [HttpDelete]
        [Route("{set}")]
        public XElement DeleteJson(string set, [FromBody]object value)
        {
            return XDataService.Delete(set, value, Request.GetQueryNameValuePairs());
        }


    }
}

