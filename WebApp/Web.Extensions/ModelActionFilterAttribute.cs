using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using System.Xml.Linq;
using System.Web.Http.Controllers;
using System.IO;
using System.Diagnostics;

namespace XData.Web.Http.Filters
{
    public class ModelActionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext.Request.Content.Headers.ContentType.MediaType == "application/json")
            {
                Debug.Assert(actionContext.ActionArguments["value"] == null);

                Stream stream = actionContext.Request.Content.ReadAsStreamAsync().Result;
                stream.Position = 0;
                StreamReader sr = new StreamReader(stream);
                string json = sr.ReadToEnd();
                sr.Close();

                string rootName = actionContext.ControllerContext.Controller.GetType().Name;
                rootName = rootName.Substring(0, rootName.Length - "Controller".Length);

                XElement element = Newtonsoft.Json.JsonConvert.DeserializeXNode(json, rootName).Elements().First();
                element.SetAttributeValue("Route", actionContext.ControllerContext.RouteData.Route.RouteTemplate);
                element.SetAttributeValue("Verb", actionContext.Request.Method.Method);       
                actionContext.ActionArguments["value"] = element;
            }
            
            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception == null)
            {
                HttpContent content = actionExecutedContext.Response.Content;
                if (content != null)
                {
                    ObjectContent objectContent = content as ObjectContent;
                    if (objectContent.Formatter is JsonMediaTypeFormatter)
                    {
                        string charSet = actionExecutedContext.Response.Content.Headers.ContentType.CharSet;
                        string mediaType = actionExecutedContext.Response.Content.Headers.ContentType.MediaType;
                        if (objectContent.Value is XElement)
                        {
                            XElement element = objectContent.Value as XElement;
                            string json = JsonConvert.SerializeXNode(element, Formatting.None, true);
                            actionExecutedContext.Response = new HttpResponseMessage { Content = new StringContent(json, Encoding.GetEncoding(charSet), mediaType) };
                        }
                    }
                }
            }

            base.OnActionExecuted(actionExecutedContext);
        }
    }
}
