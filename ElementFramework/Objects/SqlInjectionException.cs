using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Objects
{
    [Serializable]
    public class SqlInjectionException : Exception
    {
        public SqlInjectionException(string message)
            : base(message)
        {
        }

        public SqlInjectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public SqlInjectionException()
            : base("Suspected SQL-injection")
        {
        }


    }
}
