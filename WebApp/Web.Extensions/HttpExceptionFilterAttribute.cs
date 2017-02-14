using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Configuration;
using System.Web.Http.Filters;
using System.Xml.Linq;
using XData.Data;
using XData.Data.Element.Validation;
using XData.Diagnostics.Log;

namespace XData.Web.Http.Filters
{
    public class HttpExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            Log4.Logger.Error(actionExecutedContext.Exception);

            CustomErrorsSection customErrorsSection = WebConfigurationManager.GetSection("system.web/customErrors") as CustomErrorsSection;
            bool isMessageExcluded = customErrorsSection.Mode == CustomErrorsMode.On ||
                customErrorsSection.Mode == CustomErrorsMode.RemoteOnly && !actionExecutedContext.Request.IsLocal();

            string accept = actionExecutedContext.Request.GetAccept();

            if (actionExecutedContext.Exception is IElementException)
            {
                if (actionExecutedContext.Exception is ElementValidationException) isMessageExcluded = false;

                XElement error = (actionExecutedContext.Exception as IElementException).ToElement();
                if (isMessageExcluded)
                {
                    string exceptionType = error.Element("ExceptionType").Value;
                    error = new XElement(error.Name);
                    error.SetElementValue("ExceptionType", exceptionType);
                    error.SetElementValue("ExceptionMessage", "Runtime error");
                }
                if (accept == "json")
                {
                    ObjectContent content = new ObjectContent<XElement>(error, new JsonMediaTypeFormatter(), "application/json");
                    actionExecutedContext.Response = new HttpResponseMessage() { Content = content };
                }
                else
                {
                    ObjectContent content = new ObjectContent<XElement>(error, new XmlMediaTypeFormatter(), "application/xml");
                    actionExecutedContext.Response = new HttpResponseMessage() { Content = content };
                }
            }
            else
            {
                if (accept == "json")
                {
                    XElement error = new XElement("Error");
                    error.Add(new XElement("ExceptionType", actionExecutedContext.Exception.GetType().FullName));
                    if (isMessageExcluded)
                    {
                        error.Add(new XElement("ExceptionMessage", "Runtime error"));
                    }
                    else
                    {
                        error.Add(new XElement("ExceptionMessage", actionExecutedContext.Exception.Message));
                        //error.Add(new XElement("StackTrace", actionExecutedContext.Exception.StackTrace));
                    }
                    ObjectContent content = new ObjectContent<XElement>(error, new JsonMediaTypeFormatter(), "application/json");
                    actionExecutedContext.Response = new HttpResponseMessage() { Content = content };
                }
            }

            base.OnException(actionExecutedContext);
        }


    }
}