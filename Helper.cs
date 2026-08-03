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
        public static string MakeUniqueName(
    string name,
    int number,
    ISet<string> usedNames)
        {
            name = SanitizeName(name);

            string suffix = $"-{number:000}";
            int maxBaseLength = 16 - suffix.Length;

            if (maxBaseLength < 1)
                throw new InvalidOperationException("Suffix is too long.");

            string baseName = name;

            if (baseName.Length > maxBaseLength)
                baseName = baseName.Substring(0, maxBaseLength);

            string candidate = baseName + suffix;

            while (usedNames.Contains(candidate))
            {
                number++;
                suffix = $"-{number:000}";
                maxBaseLength = 16 - suffix.Length;

                baseName = name;

                if (baseName.Length > maxBaseLength)
                    baseName = baseName.Substring(0, maxBaseLength);

                candidate = baseName + suffix;
            }

            usedNames.Add(candidate);
            return candidate;
        }
    }
}
