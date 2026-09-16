using Microsoft.Extensions.Configuration;
using Rampage.ExceptionExtensions;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Rampage.Utilities
{

    /// <summary>
    /// 
    /// </summary>
    public class SecurityUtilityConfig
	{
        /// <summary>
        /// 
        /// </summary>
        public string? DES_KEY
		{
            get;
            set;
		}

        /// <summary>
        /// 
        /// </summary>
        public string? DES_IV
		{
            get;
            set;
		}

        /// <summary>
        /// 
        /// </summary>
        public string? AES_KEY
		{
            get;
            set;
		}

	}

    /// <summary>
    /// Assistant function classes for security/encryption/decryption, etc
    /// </summary>
    public sealed class SecurityTool
    {

        #region MD5

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string MD5_UTF8(string text)
        {
            return MD5(text, Encoding.UTF8);
        }

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="text">要加密的字符串</param>
        public static string MD5(string text)
        {
            return MD5(text, Encoding.Default);
        }

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="text">要加密的字符串</param>
        /// <param name="encoding"></param>
        public static string MD5(string text, Encoding encoding)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bytesMD5In = encoding.GetBytes(text);
            byte[] bytesMD5Out = md5.ComputeHash(bytesMD5In);
            string strMD5Out = BitConverter.ToString(bytesMD5Out);
            strMD5Out = strMD5Out.Replace("-", "");
            return strMD5Out;
        }

        #endregion

        #region SHA1

        /// <summary>
        /// SHA1加密
        /// </summary>
        /// <param name="text">要加密的字符串</param>
        public static string SHA1(string text)
        {
            return SHA1(text, Encoding.Default);
        }

        /// <summary>
        /// SHA1加密
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string SHA1_UTF8(string text)
        {
            return SHA1(text, Encoding.UTF8);
        }

        /// <summary>
        /// SHA1加密
        /// </summary>
        /// <param name="text">要加密的字符串</param>
        /// <param name="encoding">编码方式</param>
        public static string SHA1(string text, Encoding encoding)
        {
            SHA1 sha1 = System.Security.Cryptography.SHA1.Create();
            byte[] bytesSHA1In = encoding.GetBytes(text);
            byte[] bytesSHA1Out = sha1.ComputeHash(bytesSHA1In);
            string strSHA1Out = BitConverter.ToString(bytesSHA1Out);
            strSHA1Out = strSHA1Out.Replace("-", "");
            return strSHA1Out;
        }

        #endregion

        #region SHA256

        /// <summary>
        /// SHA256加密（UTF8编码）
        /// </summary>
        /// <param name="text"></param>
        public static string SHA256_UTF8(string text)
        {
            return SHA256(text, Encoding.UTF8);
        }

        /// <summary>
        /// SHA256加密
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        public static string SHA256(string text, Encoding encoding)
        {
            byte[] bytes = System.Security.Cryptography.SHA256.HashData(encoding.GetBytes(text));
            return Convert.ToHexString(bytes);
        }

        #endregion

        #region HMAC

        /// <summary>
        /// HMAC-SHA256签名（UTF8编码），输出十六进制小写
        /// </summary>
        /// <param name="text"></param>
        /// <param name="key"></param>
        public static string HMACSHA256(string text, string key)
        {
            return HMACSHA256(text, key, Encoding.UTF8);
        }

        /// <summary>
        /// HMAC-SHA256签名，输出十六进制小写
        /// </summary>
        /// <param name="text"></param>
        /// <param name="key"></param>
        /// <param name="encoding"></param>
        public static string HMACSHA256(string text, string key, Encoding encoding)
        {
            using HMAC hmac = new HMACSHA256(encoding.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(encoding.GetBytes(text))).ToLowerInvariant();
        }

        #endregion

        #region PBKDF2

        /// <summary>
        /// PBKDF2默认迭代次数
        /// </summary>
        public const int PBKDF2_DEFAULT_ITERATIONS = 100000;

        private const string PBKDF2_PREFIX = "PBKDF2-SHA256";

        /// <summary>
        /// 使用PBKDF2-SHA256对密码做单向哈希（每次生成随机盐），用于密码存储
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <param name="iterations">迭代次数</param>
        /// <returns>格式：PBKDF2-SHA256$迭代次数$盐(Base64)$哈希(Base64)</returns>
        public static string PBKDF2Hash(string password, int iterations = PBKDF2_DEFAULT_ITERATIONS)
        {
            if (iterations < 1000)
            {
                throw new SuperException("PBKDF2迭代次数不能低于1000");
            }
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, 32);
            return string.Concat(PBKDF2_PREFIX, "$", iterations.ToString(), "$", Convert.ToBase64String(salt), "$", Convert.ToBase64String(hash));
        }

        /// <summary>
        /// 校验明文密码与PBKDF2哈希串是否匹配（固定时间比较）
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <param name="stored">PBKDF2Hash生成的哈希串</param>
        public static bool PBKDF2Verify(string password, string? stored)
        {
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }
            string[] parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != PBKDF2_PREFIX)
            {
                return false;
            }
            if (!int.TryParse(parts[1], out int iterations) || iterations < 1000)
            {
                return false;
            }
            byte[] salt;
            byte[] storedHash;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                storedHash = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, storedHash.Length);
            return CryptographicOperations.FixedTimeEquals(hash, storedHash);
        }

        #endregion

        #region DES

        /// <summary>
        /// DES加密算法
        /// </summary>
        /// <param name="data"></param>
        [Obsolete("DES密钥长度过短已不安全，且不再提供内置默认密钥，请改用AESEncodeCBC并配置SecurityUtility:AES_KEY")]
        public static string DESEncode(string data)
        {
            var section = GlobalConfiguration.GetSection("SecurityUtility");
            SecurityUtilityConfig? config = section.Get<SecurityUtilityConfig>();
            string? key = null, iv = null;
            if (config != null)
			{
                if (!string.IsNullOrWhiteSpace(config.DES_KEY))
				{
                    key = config.DES_KEY;
                }
                if (!string.IsNullOrWhiteSpace(config.DES_IV))
				{
                    iv = config.DES_IV;
                }            
            }
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(iv))
            {
                throw new SuperException("SecurityUtility:DES_KEY/DES_IV 未配置，为避免使用可预测默认密钥已拒绝执行");
            }

            return DESEncode(key, iv, data);
        }
        /// <summary>
        /// DES加密算法
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="data"></param>
        [Obsolete("DES密钥长度过短已不安全，请改用AESEncodeCBC/AESDecodeCBC")]
        public static string DESEncode(string key, string iv, string data)
        {
            byte[] byKey = ASCIIEncoding.ASCII.GetBytes(key);
            byte[] byIV = ASCIIEncoding.ASCII.GetBytes(iv);

            var des = DES.Create();
            var encryptor = des.CreateEncryptor(byKey, byIV);
            MemoryStream ms = new MemoryStream();
            CryptoStream cst = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

            using (StreamWriter sw = new StreamWriter(cst))
            {
                sw.Write(data);
                sw.Flush();
                cst.FlushFinalBlock();
                sw.Flush();
                string result = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
                return result;
            }
        }

        /// <summary>
        /// DES解密算法
        /// </summary>
        /// <param name="data"></param>
        [Obsolete("DES密钥长度过短已不安全，且不再提供内置默认密钥，请改用AESDecodeCBC并配置SecurityUtility:AES_KEY")]
        public static string DESDecode(string data)
        {
            var section = GlobalConfiguration.GetSection("SecurityUtility");
            SecurityUtilityConfig? config = section.Get<SecurityUtilityConfig>();
            string? key = null, iv = null;
            if (config != null)
            {
                if (!string.IsNullOrWhiteSpace(config.DES_KEY))
                {
                    key = config.DES_KEY;
                }
                if (!string.IsNullOrWhiteSpace(config.DES_IV))
                {
                    iv = config.DES_IV;
                }
            }
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(iv))
            {
                throw new SuperException("SecurityUtility:DES_KEY/DES_IV 未配置，为避免使用可预测默认密钥已拒绝执行");
            }
            return DESDecode(key, iv, data);
        }
        /// <summary>
        /// DES解密算法
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <param name="data"></param>
        [Obsolete("DES密钥长度过短已不安全，请改用AESEncodeCBC/AESDecodeCBC")]
        public static string DESDecode(string key, string iv, string data)
        {
            byte[] byKey = ASCIIEncoding.ASCII.GetBytes(key);
            byte[] byIV = ASCIIEncoding.ASCII.GetBytes(iv);

            byte[] byEnc = Convert.FromBase64String(data);

            var des = DES.Create();
            var decryptor = des.CreateDecryptor(byKey, byIV);
            MemoryStream ms = new MemoryStream(byEnc);
            CryptoStream cst = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using (StreamReader sr = new StreamReader(cst))
            {
                return sr.ReadToEnd();
            }
        }

        #endregion

        #region AES

        /// <summary>
        /// AES加密函数
        /// </summary>
        /// <param name="toEncrypt"></param>
        /// <returns>加密的字符串</returns>
        [Obsolete("AES-ECB模式不安全，且不再提供内置默认密钥，请改用AESEncodeCBC并配置SecurityUtility:AES_KEY")]
        public static string AESEncode(string toEncrypt)
        {
            var section = GlobalConfiguration.GetSection("SecurityUtility");
            SecurityUtilityConfig? config = section.Get<SecurityUtilityConfig>();
            string? key = null;
            if (config != null)
            {
                if (!string.IsNullOrWhiteSpace(config.AES_KEY))
                {
                    key = config.AES_KEY;
                }
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new SuperException("SecurityUtility:AES_KEY 未配置，为避免使用可预测默认密钥已拒绝执行");
            }
            return AESEncode(toEncrypt, key);
        }
        /// <summary>
        /// AES加密函数
        /// </summary>
        /// <param name="toEncrypt"></param>
        /// <param name="key">密钥</param>
        /// <returns>加密的字符串</returns>
        [Obsolete("AES-ECB模式不安全，请改用AESEncodeCBC/AESDecodeCBC")]
        public static string AESEncode(string toEncrypt, string key)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            Aes aes = Aes.Create();
            aes.Key = keyArray;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = aes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        /// <summary>
        /// AES解密函数
        /// </summary>
        /// <param name="toDecrypt"></param>
        /// <returns></returns>
        [Obsolete("AES-ECB模式不安全，且不再提供内置默认密钥，请改用AESDecodeCBC并配置SecurityUtility:AES_KEY")]
        public static string AESDecode(string toDecrypt)
        {
            var section = GlobalConfiguration.GetSection("SecurityUtility");
            SecurityUtilityConfig? config = section.Get<SecurityUtilityConfig>();
            string? key = null;
            if (config != null)
            {
                if (!string.IsNullOrWhiteSpace(config.AES_KEY))
                {
                    key = config.AES_KEY;
                }
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new SuperException("SecurityUtility:AES_KEY 未配置，为避免使用可预测默认密钥已拒绝执行");
            }
            return AESDecode(toDecrypt, key);
        }

        /// <summary>
        /// AES解密函数
        /// </summary>
        /// <param name="toDecrypt"></param>
        /// <param name="key"></param>
        /// <returns>解密的字符串</returns>
        [Obsolete("AES-ECB模式不安全，请改用AESEncodeCBC/AESDecodeCBC")]
        public static string AESDecode(string toDecrypt, string key)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

            Aes aes = Aes.Create();
            aes.Key = keyArray;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = aes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        #endregion

        #region AES-CBC

        /// <summary>
        /// AES-CBC加密（随机IV，IV前置在密文中），密钥取配置SecurityUtility:AES_KEY
        /// </summary>
        /// <param name="toEncrypt"></param>
        /// <returns>Base64(IV+密文)</returns>
        public static string AESEncodeCBC(string toEncrypt)
        {
            var section = GlobalConfiguration.GetSection("SecurityUtility");
            SecurityUtilityConfig? config = section.Get<SecurityUtilityConfig>();
            string? key = config?.AES_KEY;
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new SuperException("SecurityUtility:AES_KEY 未配置，为避免使用可预测默认密钥已拒绝执行");
            }
            return AESEncodeCBC(toEncrypt, key);
        }

        /// <summary>
        /// AES-CBC加密（随机IV，IV前置在密文中）
        /// </summary>
        /// <param name="toEncrypt"></param>
        /// <param name="key">密钥（16/24/32字节）</param>
        /// <returns>Base64(IV+密文)</returns>
        public static string AESEncodeCBC(string toEncrypt, string key)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            byte[] iv = RandomNumberGenerator.GetBytes(aes.BlockSize / 8);
            byte[] encrypted = aes.EncryptCbc(Encoding.UTF8.GetBytes(toEncrypt), iv);
            byte[] result = new byte[iv.Length + encrypted.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// AES-CBC解密，密钥取配置SecurityUtility:AES_KEY
        /// </summary>
        /// <param name="toDecrypt">AESEncodeCBC生成的Base64串</param>
        public static string AESDecodeCBC(string toDecrypt)
        {
            var section = GlobalConfiguration.GetSection("SecurityUtility");
            SecurityUtilityConfig? config = section.Get<SecurityUtilityConfig>();
            string? key = config?.AES_KEY;
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new SuperException("SecurityUtility:AES_KEY 未配置，为避免使用可预测默认密钥已拒绝执行");
            }
            return AESDecodeCBC(toDecrypt, key);
        }

        /// <summary>
        /// AES-CBC解密
        /// </summary>
        /// <param name="toDecrypt">AESEncodeCBC生成的Base64串</param>
        /// <param name="key">密钥（16/24/32字节）</param>
        public static string AESDecodeCBC(string toDecrypt, string key)
        {
            byte[] data = Convert.FromBase64String(toDecrypt);
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            int ivSize = aes.BlockSize / 8;
            if (data.Length <= ivSize)
            {
                throw new SuperException("AES密文长度无效");
            }
            byte[] iv = data[..ivSize];
            byte[] encrypted = data[ivSize..];
            byte[] decrypted = aes.DecryptCbc(encrypted, iv);
            return Encoding.UTF8.GetString(decrypted);
        }

        #endregion

        #region Base64

        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="encode">加密采用的编码方式</param>
        /// <param name="source">待加密的明文</param>
        /// <returns></returns>
        public static string Base64Encode(string source, Encoding encode)
        {
            string result;
            byte[] bytes = encode.GetBytes(source);
            try
            {
                result = Convert.ToBase64String(bytes);
            }
            catch
            {
                result = source;
            }
            return result;
        }

        /// <summary>
        /// Base64加密，采用utf8编码方式加密
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <returns>加密后的字符串</returns>
        public static string EncodeBase64(string source)
        {
            return Base64Encode(source, Encoding.UTF8);
        }

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="encode">解密采用的编码方式，注意和加密时采用的方式一致</param>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string DecodeBase64(string result, Encoding encode)
        {
            string decode;

            try
            {
                byte[] bytes = Convert.FromBase64String(result);
                decode = encode.GetString(bytes);
            }
            catch
            {
                decode = result;
            }
            return decode;
        }

        /// <summary>
        /// Base64解密，采用utf8编码方式解密
        /// </summary>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string DecodeBase64(string result)
        {
            return DecodeBase64(result, Encoding.UTF8);
        }

        #endregion

    }
}
