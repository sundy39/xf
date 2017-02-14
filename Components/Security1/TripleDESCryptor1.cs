using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public class TripleDESCryptor1 : TripleDESCryptor
    {
        protected const string KEY = "OQ5DCUDpZ7D/EDY94Totrb/jL9bEPaeC";
        protected const string IV = "H3an/vIh8j0=";

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
