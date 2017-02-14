using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XData.Data.Objects
{
    [Serializable]
    public class UnexpectedException : Exception
    {
        public UnexpectedException(string message)
            : base(message)
        {
        }

        public UnexpectedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
