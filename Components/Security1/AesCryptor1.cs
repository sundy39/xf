using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public class AesCryptor1 : AesCryptor
    {
        protected const string KEY = "dJANDdEBc2iELLIEkrCJFI0NnuAnzAzh6lFZ5z1wios=";
        protected const string IV = "zk+FAPsuuaKYP7+bYcohRw==";

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
