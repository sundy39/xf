using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Web.Http.Filters
{
    public class HttpActionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception == null)
            {
                HttpContent content = actionExecutedContext.Response.Content;
                if (content != null)
                {
                    ObjectContent objectContent = content as ObjectContent;
                    if (objectContent.Value is XElement)
                    {
                        XElement element = objectContent.Value as XElement;

                        XAttribute attr = element.Attribute("Accept");
                        //string accept = (attr == null) ? "xml" : attr.Value;
                        if (attr != null) attr.Remove();

                        if (objectContent.Formatter is JsonMediaTypeFormatter)
                        {
                            string charSet = actionExecutedContext.Response.Content.Headers.ContentType.CharSet;
                            string mediaType = actionExecutedContext.Response.Content.Headers.ContentType.MediaType;
                            string json;
                            if (actionExecutedContext.Request.RequestUri.AbsolutePath.StartsWith("/Schemas"))
                            {
                                json = JsonConvert.ToRawJson(element);
                            }
                            else if (element.Attribute("Content") != null)
                            {
                                json = element.Attribute("Content").Value;
                            }
                            else
                            {
                                json = JsonConvert.ToJson(element);
                            }
                            actionExecutedContext.Response = new HttpResponseMessage { Content = new StringContent(json, Encoding.GetEncoding(charSet), mediaType) };
                        }
                        // else objectContent.Formatter is XmlMediaTypeFormatter
                    }
                }
            }

            base.OnActionExecuted(actionExecutedContext);
        }


    }
}