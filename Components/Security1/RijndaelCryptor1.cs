using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public class RijndaelCryptor1 : RijndaelCryptor
    {
        protected const string KEY = "PhvHK1fFhBEgxYOnXqXYvX7cQ2IlelYZYoe9v470/E4=";
        protected const string IV = "mKuf3dEWIvS1R7dfMgIBsw==";

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
