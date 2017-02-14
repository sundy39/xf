using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Schema;
using XData.Data.Extensions;

namespace System.Net.Http
{
    public static class HttpRequestMessageExtensions
    {
        public static string GetAccept(this HttpRequestMessage request)
        {
            var mediaTypes = request.Headers.Accept.Where(p => p.MediaType.Contains("/xml") || p.MediaType.Contains("/json")).OrderByDescending(p => p.Quality ?? 1);
            if (mediaTypes.Count() > 0)
            {
                var mediaType = mediaTypes.First();
                if (mediaType.MediaType.Contains("/xml"))
                {
                    string accept = request.GetQueryNameValuePairs().GetValue("accept");
                    if (string.IsNullOrWhiteSpace(accept))
                    {
                        return "xml";
                    }
                    else
                    {
                        // must be ex-xml
                        return accept;
                    }
                }
            }
            return "json";
        }


    }
}