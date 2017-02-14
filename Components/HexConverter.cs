using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data
{
    public static class HexConverter
    {
        public static string ToHexString(byte[] bytes)
        {
            string str = string.Empty;
            for (int i = 0; i < bytes.Length; i++)
            {
                str += string.Format("{0:x2}", bytes[i]);
            }
            return str;
        }

        public static byte[] ToBytes(string hexStr)
        {
            if (hexStr.Length % 2 != 0) throw new ArgumentException(hexStr);

            byte[] bytes = new byte[hexStr.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = System.Convert.ToByte(hexStr.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

    }
}
