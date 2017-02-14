using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public class RC2Cryptor1 : RC2Cryptor
    {
        protected const string KEY = "NepZXNMaXxzGUNgKJMDOEg==";
        protected const string IV = "yRTJkXjVW48=";

        protected override string GetKey()
        {
            return KEY;
        }

        protected override string GetIV()
        {
            return IV;
        }

    }
}
