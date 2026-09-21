using System;

namespace Rampage.Utilities
{
    /// <summary>字节与 Base64 字符串互转工具 / Utility for converting between byte arrays and Base64 strings</summary>
    public class ByteTool
    {

        /// <summary>
        /// 将字节数组编码为 Base64 字符串 /
        /// Encode a byte array into a Base64 string
        /// </summary>
        /// <param name="buffer">待编码的字节数组 / The byte array to encode</param>
        /// <returns>Base64 编码字符串 / The Base64-encoded string</returns>
        public static string ConvertToBase64(byte[] buffer)
        {
            string base64Str = Convert.ToBase64String(buffer);
            return base64Str;
        }

        /// <summary>
        /// 将 Base64 字符串解码为字节数组 /
        /// Decode a Base64 string into a byte array
        /// </summary>
        /// <param name="base64">待解码的 Base64 字符串 / The Base64 string to decode</param>
        /// <returns>解码后的字节数组 / The decoded byte array</returns>
        public static byte[] ConvertToBuffer(string base64)
        {
            byte[] buffer = Convert.FromBase64String(base64);
            return buffer;
        }

    }
}
