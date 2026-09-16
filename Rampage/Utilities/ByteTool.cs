using System;

namespace Rampage.Utilities
{
    public class ByteTool
    {

        public static string ConvertToBase64(byte[] buffer)
        {
            string base64Str = Convert.ToBase64String(buffer);
            return base64Str;
        }

        public static byte[] ConvertToBuffer(string base64)
        {
            byte[] buffer = Convert.FromBase64String(base64);
            return buffer;
        }

    }
}
