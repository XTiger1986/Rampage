using System;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Rampage.Utilities
{

    /// <summary>
    /// 文件大小单位（枚举值为量级序号而非字节数；相邻单位按 1024 进制换算） /
    /// File size units (the enum value is an ordinal rank, not a byte count; adjacent units convert by a factor of 1024)
    /// </summary>
    public enum FileSizeUnit : byte
    {
        /// <summary>字节 / Bytes</summary>
        B = 1,
        /// <summary>千字节（1024 B） / Kilobytes (1024 B)</summary>
        K = 2,
        /// <summary>兆字节（1024 K） / Megabytes (1024 K)</summary>
        M = 3,
        /// <summary>吉字节（1024 M） / Gigabytes (1024 M)</summary>
        G = 4,
        /// <summary>太字节（1024 G） / Terabytes (1024 G)</summary>
        T = 5,
        /// <summary>拍字节（1024 T） / Petabytes (1024 T)</summary>
        P = 6,
        /// <summary>艾字节（1024 P） / Exabytes (1024 P)</summary>
        E = 7,
        /// <summary>泽字节（1024 E） / Zettabytes (1024 E)</summary>
        Z = 8,
        /// <summary>尧字节（1024 Z） / Yottabytes (1024 Z)</summary>
        Y = 9
    }

    /// <summary>
    /// 文件工具（MD5 计算、读写、GZip 压缩、文件大小换算） /
    /// File utilities (MD5 computation, read/write, GZip compression, file size conversion)
    /// </summary>
    public class FileTool
    {

        /// <summary>
        /// 尝试删除指定路径的文件（存在则删除）/ Try to delete the file at the given path if it exists
        /// </summary>
        /// <param name="path">文件路径 / The file path</param>
        /// <returns>文件存在并已删除返回 true，不存在返回 false / true when the file existed and was deleted, false when it did not exist</returns>
        public static bool TryDeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 计算字节数组的 MD5（32 位大写十六进制） /
        /// Compute the MD5 of a byte array (32-character uppercase hex)
        /// </summary>
        /// <param name="fileBytes">文件内容字节 / The file content bytes</param>
        /// <returns>32 位大写十六进制 MD5 / The uppercase hex MD5</returns>
        public static string ComputeFileMD5Code(byte[] fileBytes)
        {
            string md5Code;

            using (Stream stream = new MemoryStream(fileBytes))
            {
                md5Code = ComputeFileMD5Code(stream);
            }

            return md5Code;
        }

        /// <summary>
        /// 计算文件的 MD5（32 位大写十六进制） /
        /// Compute a file's MD5 (32-character uppercase hex)
        /// </summary>
        /// <param name="filePath">文件路径 / The file path</param>
        /// <returns>32 位大写十六进制 MD5 / The uppercase hex MD5</returns>
        public static string ComputeFileMD5Code(string filePath)
        {
            string md5Code;

            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                md5Code = ComputeFileMD5Code(stream);
            }

            return md5Code;
        }

        /// <summary>
        /// 计算流的 MD5（32 位大写十六进制），计算完成后将流位置重置回 0 /
        /// Compute a stream's MD5 (32-character uppercase hex); the stream position is reset to 0 afterwards
        /// </summary>
        /// <param name="stream">输入流 / The input stream</param>
        /// <returns>32 位大写十六进制 MD5 / The uppercase hex MD5</returns>
        public static string ComputeFileMD5Code(Stream stream)
        {
            MD5 md5Provider = MD5.Create();
            byte[] hash_byte = md5Provider.ComputeHash(stream);

            string md5Code = System.BitConverter.ToString(hash_byte);
            md5Code = md5Code.Replace("-", "");
            stream.Position = 0;

            return md5Code;
        }

        /// <summary>
        /// 读取整个文件为字节数组（按 8KB 批次循环读取） /
        /// Read a whole file into a byte array (looped in 8KB batches)
        /// </summary>
        /// <param name="path">文件路径 / The file path</param>
        /// <returns>文件内容字节 / The file content bytes</returns>
        public static byte[] ReadFileBytes(string path)
        {
            byte[] buffer;

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                long slen = stream.Length;
                buffer = new byte[slen];
                int hasRead = 0;
                int batchSize = 1024 * 8;
                while (hasRead < slen)
                {
                    stream.Read(buffer, hasRead, batchSize);
                    hasRead += batchSize;
                }
            }

            return buffer;
        }

        /// <summary>
        /// 将字节数组写入文件 /
        /// Write a byte array to a file
        /// </summary>
        /// <param name="path">目标文件路径 / The target file path</param>
        /// <param name="fileBytes">写入的字节 / The bytes to write</param>
        /// <param name="isOverwrite">文件已存在时是否覆盖，默认 true / Whether to overwrite an existing file, true by default</param>
        /// <returns>写入成功返回 true；文件已存在且不允许覆盖返回 false / true when written; false when the file exists and overwrite is disallowed</returns>
        public static bool WriteFile(string path, byte[] fileBytes, bool isOverwrite = true)
        {
            if (File.Exists(path) && !isOverwrite)
            {
                return false;
            }
            if (File.Exists(path) && isOverwrite)
            {
                File.Delete(path);
            }
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Write(fileBytes, 0, fileBytes.Length);
            }
            return true;
        }

        /// <summary>
        /// 读取文件全文（UTF-8 编码） /
        /// Read a whole file as a string (UTF-8)
        /// </summary>
        /// <param name="path">文件路径 / The file path</param>
        /// <returns>文件内容；文件或其目录不存在返回空串 / The file content, or an empty string when the file or its directory does not exist</returns>
        public static string ReadFile(string path)
        {
            return ReadFile(path, Encoding.UTF8);
        }

        /// <summary>
        /// 读取文件全文（指定编码） /
        /// Read a whole file as a string with the given encoding
        /// </summary>
        /// <param name="path">文件路径 / The file path</param>
        /// <param name="encoding">文本编码 / The text encoding</param>
        /// <returns>文件内容；文件或其目录不存在返回空串 / The file content, or an empty string when the file or its directory does not exist</returns>
        public static string ReadFile(string path, Encoding encoding)
        {
            string content;
            string dirPath = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dirPath))
                return string.Empty;

            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path, encoding))
                {
                    content = sr.ReadToEnd();
                }
            }
            else
            {
                content = string.Empty;
            }

            return content;
        }

        /// <summary>
        /// 将字符串写入文件（UTF-8 编码，覆盖写入） /
        /// Write a string to a file (UTF-8, overwrite)
        /// </summary>
        /// <param name="path">目标文件路径 / The target file path</param>
        /// <param name="content">写入内容 / The content to write</param>
        public static void WriteFile(string path, string content)
        {
            WriteFile(path, content, Encoding.UTF8, false);
        }
        /// <summary>
        /// 将字符串写入文件（UTF-8 编码） /
        /// Write a string to a file (UTF-8)
        /// </summary>
        /// <param name="path">目标文件路径 / The target file path</param>
        /// <param name="content">写入内容 / The content to write</param>
        /// <param name="append">true 追加到文件末尾，false 覆盖原内容 / true to append, false to replace</param>
        public static void WriteFile(string path, string content, bool append)
        {
            WriteFile(path, content, Encoding.UTF8, append);
        }
        /// <summary>
        /// 将字符串写入文件（指定编码，覆盖写入） /
        /// Write a string to a file with the given encoding (overwrite)
        /// </summary>
        /// <param name="path">目标文件路径 / The target file path</param>
        /// <param name="content">写入内容 / The content to write</param>
        /// <param name="encoding">文本编码 / The text encoding</param>
        public static void WriteFile(string path, string content, Encoding encoding)
        {
            WriteFile(path, content, encoding, false);
        }
        /// <summary>
        /// 将字符串写入文件（指定编码） /
        /// Write a string to a file with the given encoding
        /// </summary>
        /// <remarks>目标目录不存在时自动创建。The target directory is created when missing.</remarks>
        /// <param name="path">目标文件路径 / The target file path</param>
        /// <param name="content">写入内容 / The content to write</param>
        /// <param name="encoding">文本编码 / The text encoding</param>
        /// <param name="append">true 追加到文件末尾，false 覆盖原内容 / true to append, false to replace</param>
        public static void WriteFile(string path, string content, Encoding encoding, bool append)
        {
            string dirPath = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            using (StreamWriter sw = new StreamWriter(path, append, encoding))
            {
                sw.Write(content);
            }
        }



        /// <summary>
        /// 将数值大小按单位换算为字节数（1024 进制） /
        /// Convert a size value with its unit into a byte length (base 1024)
        /// </summary>
        /// <remarks>结果经 Convert.ToInt64 取整（中间值向偶数舍入）。The result is truncated to long via Convert.ToInt64 (midpoints round to even).</remarks>
        /// <param name="sizeNum">数值大小 / The size value</param>
        /// <param name="unit">单位 / The unit</param>
        /// <returns>字节数 / The byte length</returns>
        public static long GetByteLength(decimal sizeNum, FileSizeUnit unit)
        {
            long byteLen = 0;
            switch (unit)
            {
                case FileSizeUnit.Y:
                    byteLen = Convert.ToInt64(sizeNum * 1024 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024);
                    break;
                case FileSizeUnit.Z:
                    byteLen = Convert.ToInt64(sizeNum * 1024 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024);
                    break;
                case FileSizeUnit.E:
                    byteLen = Convert.ToInt64(sizeNum * 1024 * 1024 * 1024 * 1024 * 1024 * 1024);
                    break;
                case FileSizeUnit.P:
                    byteLen = Convert.ToInt64(sizeNum * 1024 * 1024 * 1024 * 1024 * 1024);
                    break;
                case FileSizeUnit.T:
                    byteLen = Convert.ToInt64(sizeNum * 1024 * 1024 * 1024 * 1024);
                    break;
                case FileSizeUnit.G:
                    byteLen = Convert.ToInt64(sizeNum * 1024 * 1024 * 1024);
                    break;
                case FileSizeUnit.M:
                    byteLen = Convert.ToInt64(sizeNum * 1024 * 1024);
                    break;
                case FileSizeUnit.K:
                    byteLen = Convert.ToInt64(sizeNum * 1024);
                    break;
                case FileSizeUnit.B:
                    byteLen = Convert.ToInt64(sizeNum);
                    break;
                default:
                    byteLen = Convert.ToInt64(sizeNum);
                    break;
            }

            return byteLen;
        }

        /// <summary>
        /// 将数值大小按单位字符串换算为字节数（1024 进制） /
        /// Convert a size value with its unit string into a byte length (base 1024)
        /// </summary>
        /// <remarks>单位大小写不敏感；未识别的单位（含 default 分支）按字节 B 处理。Unit matching is case-insensitive; unrecognized units (including the default branch) fall back to bytes (B).</remarks>
        /// <param name="sizeNum">数值大小 / The size value</param>
        /// <param name="unit">单位：T|TB、G|GB、M|MB、K|KB、B|BS|BYTE|BYTES 等 / The unit: T|TB, G|GB, M|MB, K|KB, B|BS|BYTE|BYTES, etc.</param>
        /// <returns>字节数 / The byte length</returns>
        public static long GetByteLength(decimal sizeNum, string unit)
        {
            FileSizeUnit eu = FileSizeUnit.B;
            switch (unit.ToUpper())
            {
                case "Y":
                case "YB":
                    eu = FileSizeUnit.Y;
                    break;
                case "Z":
                case "ZB":
                    eu= FileSizeUnit.Z;
                    break;
                case "E":
                case "EB":
                    eu = FileSizeUnit.E;
                    break;
                case "P":
                case "PB":
                    eu = FileSizeUnit.P;
                    break;
                case "T":
                case "TB":
                    eu = FileSizeUnit.T;
                    break;
                case "G":
                case "GB":
                    eu = FileSizeUnit.G;
                    break;
                case "M":
                case "MB":
                    eu = FileSizeUnit.M;
                    break;
                case "K":
                case "KB":
                    eu = FileSizeUnit.K;
                    break;
                case "B":
                case "BS":
                case "BYTE":
                case "BYTES":
                default:
                    eu = FileSizeUnit.B;
                    break;
            }

            return GetByteLength(sizeNum, eu);
        }

        private static readonly Regex _sizeRegex = new Regex(@"^(?<Size>[0-9]+(\.[0-9]+)?)\s*(?<Unit>[a-zA-Z]+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        /// <summary>
        /// 解析"数值+单位"形式的文件大小串为字节数（如 "168KB"、"588MB"） /
        /// Parse a "number + unit" size string into a byte length (e.g. "168KB", "588MB")
        /// </summary>
        /// <remarks>数字与单位间允许空白；不匹配正则时返回 0。Whitespace between the number and unit is allowed; returns 0 when the string does not match.</remarks>
        /// <param name="sizeInfo">大小串 / The size string</param>
        /// <returns>字节数，无法解析时为 0 / The byte length, or 0 when unparsable</returns>
        public static long GetByteLengthFromSizeString(string sizeInfo)
        {
            long byteLen = 0;

            if (_sizeRegex.IsMatch(sizeInfo))
            {
                Match sizeMatch = _sizeRegex.Match(sizeInfo);
                GroupCollection sizeMatchGroups = sizeMatch.Groups;
                Group sizeMatchGroup = sizeMatchGroups["Size"];
                Group unitMatchGroup = sizeMatchGroups["Unit"];

                decimal sizeNum = StringTool.TryParse<decimal>(sizeMatchGroup.Value, 0);
                string unit = unitMatchGroup.Value;
                byteLen = GetByteLength(sizeNum, unit);
            }

            return byteLen;
        }


        /// <summary>
        /// 解压 GZip 字节并按指定编码读取为字符串 /
        /// Decompress GZip bytes and read them as a string with the given encoding
        /// </summary>
        /// <param name="bytes">GZip 压缩字节 / The GZip-compressed bytes</param>
        /// <param name="encoding">文本编码 / The text encoding</param>
        /// <returns>解压后的字符串 / The decompressed string</returns>
        public static string ReadGZip(byte[] bytes, Encoding encoding)
        {
            string result = string.Empty;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using (GZipStream decompressedStream = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (StreamReader sr = new StreamReader(decompressedStream, encoding))
                    {
                        result = sr.ReadToEnd();
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 将字符串按指定编码压缩为 GZip 字节数组 /
        /// Compress a string into GZip bytes with the given encoding
        /// </summary>
        /// <param name="input">待压缩字符串 / The string to compress</param>
        /// <param name="encoding">文本编码 / The text encoding</param>
        /// <returns>GZip 压缩字节 / The GZip-compressed bytes</returns>
        public static byte[] WriteGZip(string input, Encoding encoding)
        {
            byte[] rawData = encoding.GetBytes(input);
            using (MemoryStream ms = new MemoryStream())
            {
                GZipStream compressedzipStream = new GZipStream(ms, CompressionMode.Compress, true);
                compressedzipStream.Write(rawData, 0, rawData.Length);
                compressedzipStream.Close();
                return ms.ToArray();
            }
        }


    }
}
