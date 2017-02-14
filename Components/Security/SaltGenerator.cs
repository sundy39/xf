using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public class SaltGenerator
    {
        public const int DEFAULT_SALT_LENGTH = 16;

        public string GenerateSalt()
        {
            return GenerateSalt(DEFAULT_SALT_LENGTH);
        }

        public string GenerateSalt(int saltLength)
        {
            byte[] buffer = new byte[saltLength];
            (new RNGCryptoServiceProvider()).GetBytes(buffer);
            return Convert.ToBase64String(buffer);
        }


    }
}
