using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaymarineConverter
{
    internal static class Helper
    {
        public static byte[] PadTo512(byte[] data)
        {
            int pad = 512 - (data.Length % 512);
            if (pad == 512) return data;

            var outBuf = new byte[data.Length + pad];
            Buffer.BlockCopy(data, 0, outBuf, 0, data.Length);
            return outBuf;
        }

        public static byte[] GetBytes(string s, int length)
        {
            var b = new byte[length];
            var data = System.Text.Encoding.ASCII.GetBytes(s);
            Array.Copy(data, b, Math.Min(length, data.Length));
            return b;
        }

        public static byte[] DoubleBytes(double d)
        {
            return BitConverter.GetBytes(d);
        }
        public static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "WP";

            name = name.Trim();

            if (name.Length > 16)
                name = name.Substring(0, 16);

            return name.Replace(",", " ");
        }
    }
}
