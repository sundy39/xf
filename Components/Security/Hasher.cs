using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public abstract class Hasher : IEncryptor
    {
        public virtual string Encrypt(string text)
        {
            return Encrypt(text, null);
        }

        public abstract string Encrypt(string text, string salt);

        public string Hash(string text)
        {
            return Hash(text, null);
        }

        public string Hash(string text, string salt)
        {
            return Encrypt(text, salt);
        }

        protected string Hash(HashAlgorithm hashAlgorithm, string plainText, string salt)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] buffer;
            if (string.IsNullOrEmpty(salt))
            {
                buffer = plainBytes;
            }
            else
            {
                byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
                buffer = new byte[saltBytes.Length + plainBytes.Length];

                Buffer.BlockCopy(saltBytes, 0, buffer, 0, saltBytes.Length);
                Buffer.BlockCopy(plainBytes, 0, buffer, saltBytes.Length, plainBytes.Length);
            }

            byte[] result = hashAlgorithm.ComputeHash(buffer);
            return HexConverter.ToHexString(result);
        }


    }
}
