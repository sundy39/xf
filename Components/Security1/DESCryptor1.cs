using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public class DESCryptor1 : DESCryptor
    {
        protected const string KEY = "f9Sepo7BOho=";
        protected const string IV = "ghQ7ysLG+6I=";

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
