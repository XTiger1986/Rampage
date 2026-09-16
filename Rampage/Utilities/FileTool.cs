using System;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Rampage.Utilities
{

    public enum FileSizeUnit : byte
    {
        B = 1,
        K = 2,
        M = 3,
        G = 4,
        T = 5,
        P = 6,
        E = 7,
        Z = 8,
        Y = 9
    }

    /// <summary>
    /// 
    /// </summary>
    public class FileTool
    {

        /// <summary>
        /// 尝试删除指定路径文件(如果存在的话)
        /// try delete file if exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns>if not exists, return false</returns>
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
        /// get file md5 from file bytes
        /// </summary>
        /// <param name="fileBytes"></param>
        /// <returns></returns>
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
        /// get file md5 from file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
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
        ///  get file md5 from file stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
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
        /// read bytes from file path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileBytes"></param>
        /// <param name="isOverwrite"></param>
        /// <returns></returns>
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
        /// read file string from file path (use UTF8)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ReadFile(string path)
        {
            return ReadFile(path, Encoding.UTF8);
        }

        /// <summary>
        /// read file string from file path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
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
        /// write string content to file (use UTF8)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        public static void WriteFile(string path, string content)
        {
            WriteFile(path, content, Encoding.UTF8, false);
        }
        /// <summary>
        /// write string content to file (use UTF8)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        /// <param name="append">is append to file, otherwise replaced</param>
        public static void WriteFile(string path, string content, bool append)
        {
            WriteFile(path, content, Encoding.UTF8, append);
        }
        /// <summary>
        /// write string content to file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        public static void WriteFile(string path, string content, Encoding encoding)
        {
            WriteFile(path, content, encoding, false);
        }
        /// <summary>
        /// write string content to file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        /// <param name="append"></param>
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
        /// get byte value from file size info
        /// </summary>
        /// <param name="sizeNum">size value</param>
        /// <param name="unit"></param>
        /// <returns></returns>
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
        /// get byte value from file size info
        /// </summary>
        /// <param name="sizeNum">size value</param>
        /// <param name="unit">unit：T|TB、G|GB、M|MB、K|KB、B|BS|BYTE|BYTES...</param>
        /// <returns></returns>
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
        /// get byte value from file size info string
        /// </summary>
        /// <param name="sizeInfo">for example：168KB、588MB</param>
        /// <returns></returns>
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
        /// read string from GZip bytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
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
        /// get GZip bytes from string
        /// </summary>
        /// <param name="input"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
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
