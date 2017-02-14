using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Security
{
    public interface IEncryptor
    {
        string Encrypt(string text);
        string Encrypt(string text, string salt);
    }
}
