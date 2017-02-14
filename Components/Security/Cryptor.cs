using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public abstract class Cryptor : IEncryptor
    {
        public virtual string Encrypt(string text)
        {
            return Encrypt(text, null);
        }

        public virtual string Encrypt(string text, string salt)
        {
            string saltedText = AddSalt(text, salt);
            SymmetricAlgorithm algorithm = GetSymmetricAlgorithm();
            return Encrypt(algorithm, saltedText);
        }

        public virtual string Decrypt(string encryptedText)
        {
            return Decrypt(encryptedText, null);
        }

        public virtual string Decrypt(string encryptedText, string salt)
        {
            SymmetricAlgorithm algorithm = GetSymmetricAlgorithm();
            string saltedText = Decrypt(algorithm, encryptedText);
            saltedText = saltedText.TrimEnd('\0');
            return RemoveSalt(saltedText, salt);
        }

        protected virtual SymmetricAlgorithm GetSymmetricAlgorithm()
        {
            SymmetricAlgorithm algorithm = CreateSymmetricAlgorithm();
            algorithm.Key = Convert.FromBase64String(GetKey());
            algorithm.IV = Convert.FromBase64String(GetIV());
            return algorithm;
        }

        protected virtual string AddSalt(string text, string salt)
        {
            if (string.IsNullOrEmpty(salt)) return text;
            string[] array = SplitSalt(salt);
            return array[0] + text + array[1];
        }

        protected virtual string RemoveSalt(string saltedText, string salt)
        {
            if (string.IsNullOrEmpty(salt)) return saltedText;
            string[] array = SplitSalt(salt);
            return saltedText.Substring(array[0].Length, saltedText.Length - array[0].Length - array[1].Length);
        }

        private string[] SplitSalt(string salt)
        {
            string[] result = new string[2];
            int length = salt.Length;
            if (length == 0)
            {
                result[0] = string.Empty;
                result[1] = string.Empty;
            }
            else if (length == 1)
            {
                result[0] = salt[0].ToString();
                result[1] = string.Empty;
            }
            else if (length == 2)
            {
                result[0] = salt[0].ToString();
                result[1] = salt[1].ToString();
            }
            else if (length == 3)
            {
                result[0] = salt[0].ToString();
                result[1] = salt[1].ToString() + salt[2].ToString();
            }
            else
            {
                // length >= 4
                result[0] = salt.Substring(0, salt.Length / 2);
                result[1] = salt.Substring(salt.Length / 2);
            }
            return result;
        }

        protected abstract SymmetricAlgorithm CreateSymmetricAlgorithm();
        protected abstract string GetKey();
        protected abstract string GetIV();

        protected static string Encrypt(SymmetricAlgorithm algorithm, string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, algorithm.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                }
                byte[] cipherBytes = ms.ToArray();
                return HexConverter.ToHexString(cipherBytes);
            }
        }

        protected static string Decrypt(SymmetricAlgorithm algorithm, string encrypted)
        {
            byte[] cipherBytes = HexConverter.ToBytes(encrypted);

            using (MemoryStream memoryStream = new MemoryStream(cipherBytes))
            {
                using (CryptoStream cs = new CryptoStream(memoryStream,
                    algorithm.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    byte[] plainBytes = new byte[cipherBytes.Length];
                    cs.Read(plainBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }


    }
}
