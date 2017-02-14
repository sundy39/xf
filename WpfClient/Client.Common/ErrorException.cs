using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Client.Common
{
    [Serializable]
    public class ErrorException : Exception
    {
        public XElement Error { get; private set; }

        public ErrorException(XElement error)
               : base()
        {
            Error = error;
        }

        public ErrorException(string message, XElement error)
             : base(message)
        {
            Error = error;
        }


    }
}
