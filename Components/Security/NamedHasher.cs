using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public abstract class NamedHasher : Hasher
    {
        protected readonly string HashName;

        private NamedHasher()
        {
        }

        public NamedHasher(string hashName)
        {
            HashName = hashName;
        }

        public override string Encrypt(string text, string salt)
        {
            HashAlgorithm hashAlgorithm = HashAlgorithm.Create(HashName);
            return Hash(hashAlgorithm, text, salt);
        }


    }

    public class SHA1Hasher: NamedHasher
    {
        public SHA1Hasher()
            :base("SHA1")
        {
        }
    }

    public class MD5Hasher: NamedHasher
    {
        public MD5Hasher()
             : base("MD5")
        {
        }
    }

    public class SHA256Hasher : NamedHasher
    {
        public SHA256Hasher()
             : base("SHA256")
        {
        }
    }

    public class SHA384Hasher : NamedHasher
    {
        public SHA384Hasher()
             : base("SHA384")
        {
        }
    }

    public class SHA512Hasher : NamedHasher
{
        public SHA512Hasher()
             : base("SHA512")
        {
        }
    }

}
