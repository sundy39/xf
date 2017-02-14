using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Client.Common;

namespace XData.Client.Components
{
    [Serializable]
    public class HttpResponseException : ErrorException
    {
        public HttpStatusCode StatusCode { get; set; }


        public HttpResponseException(HttpStatusCode statusCode, XElement error)
            : base(error)
        {
            StatusCode = statusCode;
            error.SetAttributeValue("StatusCode", (int)statusCode);
        }

        public HttpResponseException(string message, HttpStatusCode statusCode, XElement error)
            : base(message, error)
        {
            StatusCode = statusCode;
            error.SetAttributeValue("StatusCode", (int)statusCode);
        }


    }
}
