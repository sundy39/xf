using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public abstract class DESCryptor : Cryptor
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            // Mode CBC; Padding PKCS7; Key	byte[8]; IV byte[8]
            SymmetricAlgorithm algorithm = new DESCryptoServiceProvider();
            return algorithm;
        }
    }

    public abstract class RC2Cryptor : Cryptor
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            // Mode CBC; Padding PKCS7; Key	byte[16]; IV byte[8]
            SymmetricAlgorithm algorithm = new RC2CryptoServiceProvider();
            return algorithm;
        }
    }

    public abstract class AesCryptor : Cryptor
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            // Mode CBC; Padding PKCS7; Key	byte[32]; IV byte[16]
            SymmetricAlgorithm algorithm = new AesCryptoServiceProvider();
            return algorithm;
        }
    }

    public abstract class RijndaelCryptor : Cryptor
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            // Mode CBC; Padding PKCS7; Key	byte[32]; IV byte[16]
            SymmetricAlgorithm algorithm = new RijndaelManaged();
            return algorithm;
        }
    }

    public abstract class TripleDESCryptor : Cryptor
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            // Mode CBC; Padding PKCS7; Key	byte[24]; IV byte[8]
            SymmetricAlgorithm algorithm = new TripleDESCryptoServiceProvider();
            return algorithm;
        }
    }

}
