using Microsoft.Extensions.Configuration;
using Rampage.ExceptionExtensions;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Rampage.Utilities
{

    /// <summary>
    /// 安全工具配置项（绑定配置节 SecurityUtility，供 DES/AES 的无参重载读取密钥） /
    /// Security utility configuration (binds the SecurityUtility config section; supplies keys to the parameterless DES/AES overloads)
    /// </summary>
    public class SecurityUtilityConfig
	{
        /// <summary>DES 密钥（ASCII 字符串，须为 8 字符）/ DES key (an 8-character ASCII string)</summary>
        public string? DES_KEY
		{
            get;
            set;
		}

        /// <summary>DES 初始向量（ASCII 字符串，须为 8 字符）/ DES initialization vector (an 8-character ASCII string)</summary>
        public string? DES_IV
		{
            get;
            set;
		}

        /// <summary>AES 密钥（按 UTF-8 字节数须为 16/24/32 字节）/ AES key (its UTF-8 byte length must be 16/24/32 bytes)</summary>
        public string? AES_KEY
		{
            get;
            set;
		}

	}

    /// <summary>
    /// 安全/加密/解密辅助工具类 /
    /// Helper class for security, hashing, encryption and decryption
    /// </summary>
    /// <remarks>
    /// 输出格式差异：MD5/SHA1/SHA256 返回大写十六进制；HMACSHA256 返回小写十六进制；
    /// PBKDF2-SHA256 返回 "PBKDF2-SHA256$迭代次数$盐(Base64)$哈希(Base64)" 结构串并以固定时间比较校验
    /// （默认 10 万次迭代、16 字节盐、32 字节哈希）。DES 与 AES-ECB 已标记 Obsolete；
    /// 无密钥重载从配置节 SecurityUtility 读取密钥，未配置时直接抛异常而非回退内置默认密钥。
    /// Output formats differ: MD5/SHA1/SHA256 return uppercase hex while HMACSHA256 returns lowercase hex;
    /// PBKDF2-SHA256 returns a "PBKDF2-SHA256$iterations$salt(Base64)$hash(Base64)" string verified with a
    /// fixed-time comparison (100,000 iterations, a 16-byte salt and a 32-byte hash by default). DES and
    /// AES-ECB are marked obsolete; the keyless overloads read keys from the SecurityUtility config section
    /// and throw when unconfigured instead of falling back to a built-in default key.
    /// </remarks>
    public sealed class SecurityTool
    {

        #region MD5

        /// <summary>
        /// MD5 加密（UTF-8 编码），返回 32 位大写十六进制 /
        /// MD5 hash (UTF-8 encoded), returned as a 32-character uppercase hex string
        /// </summary>
        /// <param name="text">要加密的字符串 / The string to hash</param>
        /// <returns>32 位大写十六进制哈希 / The uppercase hex hash</returns>
        public static string MD5_UTF8(string text)
        {
            return MD5(text, Encoding.UTF8);
        }

        /// <summary>
        /// MD5 加密（默认编码 Encoding.Default），返回 32 位大写十六进制 /
        /// MD5 hash (default encoding Encoding.Default), returned as a 32-character uppercase hex string
        /// </summary>
        /// <remarks>
        /// .NET 8 中 Encoding.Default 恒为 UTF-8，与 <see cref="MD5_UTF8"/> 结果一致；
        /// .NET Framework 下则为系统 ANSI 代码页，结果可能不同。需要跨平台稳定结果时请显式指定编码。
        /// On .NET 8 Encoding.Default is always UTF-8, so this matches <see cref="MD5_UTF8"/>;
        /// on .NET Framework it is the system ANSI code page and may differ. Pass an explicit encoding when stable results are required.
        /// </remarks>
        /// <param name="text">要加密的字符串 / The string to hash</param>
        /// <returns>32 位大写十六进制哈希 / The uppercase hex hash</returns>
        public static string MD5(string text)
        {
            return MD5(text, Encoding.Default);
        }

        /// <summary>
        /// MD5 加密（指定编码），返回 32 位大写十六进制 /
        /// MD5 hash with the given encoding, returned as a 32-character uppercase hex string
        /// </summary>
        /// <param name="text">要加密的字符串 / The string to hash</param>
        /// <param name="encoding">字符串编码 / The string encoding</param>
        /// <returns>32 位大写十六进制哈希 / The uppercase hex hash</returns>
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
        /// SHA1 加密（默认编码 Encoding.Default），返回 40 位大写十六进制 /
        /// SHA1 hash (default encoding Encoding.Default), returned as a 40-character uppercase hex string
        /// </summary>
        /// <remarks>
        /// .NET 8 中 Encoding.Default 恒为 UTF-8，与 <see cref="SHA1_UTF8"/> 结果一致；
        /// .NET Framework 下则为系统 ANSI 代码页，结果可能不同。需要跨平台稳定结果时请显式指定编码。
        /// On .NET 8 Encoding.Default is always UTF-8, so this matches <see cref="SHA1_UTF8"/>;
        /// on .NET Framework it is the system ANSI code page and may differ. Pass an explicit encoding when stable results are required.
        /// </remarks>
        /// <param name="text">要加密的字符串 / The string to hash</param>
        /// <returns>40 位大写十六进制哈希 / The uppercase hex hash</returns>
        public static string SHA1(string text)
        {
            return SHA1(text, Encoding.Default);
        }

        /// <summary>
        /// SHA1 加密（UTF-8 编码），返回 40 位大写十六进制 /
        /// SHA1 hash (UTF-8 encoded), returned as a 40-character uppercase hex string
        /// </summary>
        /// <param name="text">要加密的字符串 / The string to hash</param>
        /// <returns>40 位大写十六进制哈希 / The uppercase hex hash</returns>
        public static string SHA1_UTF8(string text)
        {
            return SHA1(text, Encoding.UTF8);
        }

        /// <summary>
        /// SHA1 加密（指定编码），返回 40 位大写十六进制 /
        /// SHA1 hash with the given encoding, returned as a 40-character uppercase hex string
        /// </summary>
        /// <param name="text">要加密的字符串 / The string to hash</param>
        /// <param name="encoding">编码方式 / The string encoding</param>
        /// <returns>40 位大写十六进制哈希 / The uppercase hex hash</returns>
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
        /// SHA256 加密（UTF-8 编码），返回 64 位大写十六进制 /
        /// SHA256 hash (UTF-8 encoded), returned as a 64-character uppercase hex string
        /// </summary>
        /// <param name="text">要哈希的字符串 / The string to hash</param>
        /// <returns>64 位大写十六进制哈希 / The uppercase hex hash</returns>
        public static string SHA256_UTF8(string text)
        {
            return SHA256(text, Encoding.UTF8);
        }

        /// <summary>
        /// SHA256 加密（指定编码），返回 64 位大写十六进制 /
        /// SHA256 hash with the given encoding, returned as a 64-character uppercase hex string
        /// </summary>
        /// <param name="text">要哈希的字符串 / The string to hash</param>
        /// <param name="encoding">编码方式 / The string encoding</param>
        /// <returns>64 位大写十六进制哈希 / The uppercase hex hash</returns>
        public static string SHA256(string text, Encoding encoding)
        {
            byte[] bytes = System.Security.Cryptography.SHA256.HashData(encoding.GetBytes(text));
            return Convert.ToHexString(bytes);
        }

        #endregion

        #region HMAC

        /// <summary>
        /// HMAC-SHA256 签名（UTF-8 编码），输出十六进制小写 /
        /// HMAC-SHA256 signature (UTF-8), returned as lowercase hex
        /// </summary>
        /// <param name="text">待签名内容 / The content to sign</param>
        /// <param name="key">签名密钥（按 UTF-8 编码） / The signing key (encoded as UTF-8)</param>
        /// <returns>小写十六进制签名 / The lowercase hex signature</returns>
        public static string HMACSHA256(string text, string key)
        {
            return HMACSHA256(text, key, Encoding.UTF8);
        }

        /// <summary>
        /// HMAC-SHA256 签名（指定编码，密钥与内容使用同一编码），输出十六进制小写 /
        /// HMAC-SHA256 signature (given encoding, used for both key and content), returned as lowercase hex
        /// </summary>
        /// <param name="text">待签名内容 / The content to sign</param>
        /// <param name="key">签名密钥 / The signing key</param>
        /// <param name="encoding">编码方式 / The encoding</param>
        /// <returns>小写十六进制签名 / The lowercase hex signature</returns>
        public static string HMACSHA256(string text, string key, Encoding encoding)
        {
            using HMAC hmac = new HMACSHA256(encoding.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(encoding.GetBytes(text))).ToLowerInvariant();
        }

        #endregion

        #region PBKDF2

        /// <summary>PBKDF2 默认迭代次数（100000 次） / The default PBKDF2 iteration count (100,000)</summary>
        public const int PBKDF2_DEFAULT_ITERATIONS = 100000;

        private const string PBKDF2_PREFIX = "PBKDF2-SHA256";

        /// <summary>
        /// 使用 PBKDF2-SHA256 对密码做单向哈希（每次生成随机盐），用于密码存储 /
        /// One-way hash a password with PBKDF2-SHA256 (a fresh random salt each call), for password storage
        /// </summary>
        /// <remarks>盐为 16 随机字节，哈希为 32 字节；迭代次数低于 1000 抛 <see cref="SuperException"/>。The salt is 16 random bytes and the hash 32 bytes; an iteration count below 1000 throws <see cref="SuperException"/>.</remarks>
        /// <param name="password">明文密码 / The plaintext password</param>
        /// <param name="iterations">迭代次数，默认 PBKDF2_DEFAULT_ITERATIONS（100000） / Iteration count, PBKDF2_DEFAULT_ITERATIONS (100,000) by default</param>
        /// <returns>格式：PBKDF2-SHA256$迭代次数$盐(Base64)$哈希(Base64) / Format: PBKDF2-SHA256$iterations$salt(Base64)$hash(Base64)</returns>
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
        /// 校验明文密码与 PBKDF2 哈希串是否匹配（CryptographicOperations.FixedTimeEquals 固定时间比较，防时序攻击） /
        /// Verify a plaintext password against a PBKDF2 hash string (constant-time comparison via CryptographicOperations.FixedTimeEquals, resisting timing attacks)
        /// </summary>
        /// <remarks>按存储的盐与迭代次数重算哈希后比较；哈希串格式非法、Base64 不合法或迭代次数低于 1000 时直接返回 false 而不抛异常。The hash is recomputed with the stored salt and iterations; a malformed string, invalid Base64 parts or an iteration count below 1000 return false instead of throwing.</remarks>
        /// <param name="password">明文密码 / The plaintext password</param>
        /// <param name="stored">PBKDF2Hash 生成的哈希串 / The hash string produced by PBKDF2Hash</param>
        /// <returns>匹配返回 true / true when the password matches</returns>
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
        /// DES 加密（密钥取配置 SecurityUtility:DES_KEY/DES_IV，未配置时抛异常拒绝执行） /
        /// DES encryption (key/IV come from the SecurityUtility:DES_KEY/DES_IV config; throws when unconfigured)
        /// </summary>
        /// <param name="data">明文 / The plaintext</param>
        /// <returns>Base64 密文 / The Base64 ciphertext</returns>
        /// <exception cref="SuperException">配置节未配置 DES_KEY 或 DES_IV / DES_KEY or DES_IV is not configured</exception>
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
        /// DES 加密（密钥与 IV 按 ASCII 取字节，均须为 8 字符，否则抛 CryptographicException），输出 Base64 /
        /// DES encryption (the key and IV are taken as ASCII bytes and must each be 8 characters, otherwise a CryptographicException is thrown), output is Base64
        /// </summary>
        /// <param name="key">DES 密钥（8 个 ASCII 字符） / The DES key (8 ASCII characters)</param>
        /// <param name="iv">DES 初始向量（8 个 ASCII 字符） / The DES IV (8 ASCII characters)</param>
        /// <param name="data">明文 / The plaintext</param>
        /// <returns>Base64 密文 / The Base64 ciphertext</returns>
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
        /// DES 解密（密钥取配置 SecurityUtility:DES_KEY/DES_IV，未配置时抛异常拒绝执行） /
        /// DES decryption (key/IV come from the SecurityUtility:DES_KEY/DES_IV config; throws when unconfigured)
        /// </summary>
        /// <param name="data">Base64 密文 / The Base64 ciphertext</param>
        /// <returns>解密后的明文 / The decrypted plaintext</returns>
        /// <exception cref="SuperException">配置节未配置 DES_KEY 或 DES_IV / DES_KEY or DES_IV is not configured</exception>
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
        /// DES 解密（密钥与 IV 按 ASCII 取字节，均须为 8 字符），输入 Base64 密文 /
        /// DES decryption (the key and IV are taken as ASCII bytes and must each be 8 characters); the input is Base64 ciphertext
        /// </summary>
        /// <param name="key">DES 密钥（8 个 ASCII 字符） / The DES key (8 ASCII characters)</param>
        /// <param name="iv">DES 初始向量（8 个 ASCII 字符） / The DES IV (8 ASCII characters)</param>
        /// <param name="data">Base64 密文 / The Base64 ciphertext</param>
        /// <returns>解密后的明文 / The decrypted plaintext</returns>
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
        /// AES-ECB+PKCS7 加密（密钥取配置 SecurityUtility:AES_KEY，未配置时抛异常拒绝执行） /
        /// AES-ECB+PKCS7 encryption (the key comes from the SecurityUtility:AES_KEY config; throws when unconfigured)
        /// </summary>
        /// <param name="toEncrypt">明文（UTF-8） / The plaintext (UTF-8)</param>
        /// <returns>Base64 密文 / The Base64 ciphertext</returns>
        /// <exception cref="SuperException">配置节未配置 AES_KEY / AES_KEY is not configured</exception>
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
        /// AES-ECB+PKCS7 加密：密钥按 UTF-8 字节数须为 16/24/32 字节（其它长度抛 CryptographicException），输出 Base64 /
        /// AES-ECB+PKCS7 encryption: the key's UTF-8 byte length must be 16/24/32 bytes (other lengths throw a CryptographicException); output is Base64
        /// </summary>
        /// <param name="toEncrypt">明文（UTF-8） / The plaintext (UTF-8)</param>
        /// <param name="key">密钥（16/24/32 字节） / The key (16/24/32 bytes)</param>
        /// <returns>Base64 密文 / The Base64 ciphertext</returns>
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
        /// AES-ECB+PKCS7 解密（密钥取配置 SecurityUtility:AES_KEY，未配置时抛异常拒绝执行） /
        /// AES-ECB+PKCS7 decryption (the key comes from the SecurityUtility:AES_KEY config; throws when unconfigured)
        /// </summary>
        /// <param name="toDecrypt">Base64 密文 / The Base64 ciphertext</param>
        /// <returns>解密后的明文（UTF-8） / The decrypted plaintext (UTF-8)</returns>
        /// <exception cref="SuperException">配置节未配置 AES_KEY / AES_KEY is not configured</exception>
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
        /// AES-ECB+PKCS7 解密：密钥按 UTF-8 字节数须为 16/24/32 字节（其它长度抛 CryptographicException），输入 Base64 密文 /
        /// AES-ECB+PKCS7 decryption: the key's UTF-8 byte length must be 16/24/32 bytes (other lengths throw a CryptographicException); the input is Base64 ciphertext
        /// </summary>
        /// <param name="toDecrypt">Base64 密文 / The Base64 ciphertext</param>
        /// <param name="key">密钥（16/24/32 字节） / The key (16/24/32 bytes)</param>
        /// <returns>解密后的明文（UTF-8） / The decrypted plaintext (UTF-8)</returns>
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
        /// AES-CBC 加密（随机 IV，IV 前置在密文中），密钥取配置 SecurityUtility:AES_KEY /
        /// AES-CBC encryption (random IV prepended to the ciphertext); the key comes from the SecurityUtility:AES_KEY config
        /// </summary>
        /// <param name="toEncrypt">明文（UTF-8） / The plaintext (UTF-8)</param>
        /// <returns>Base64(IV+密文) / Base64(IV + ciphertext)</returns>
        /// <exception cref="SuperException">配置节未配置 AES_KEY / AES_KEY is not configured</exception>
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
        /// AES-CBC 加密（每次随机生成 IV 并前置在密文中） /
        /// AES-CBC encryption (a fresh random IV is generated per call and prepended to the ciphertext)
        /// </summary>
        /// <remarks>密钥按 UTF-8 字节数计须为 16/24/32 字节，否则抛 CryptographicException。The key's UTF-8 byte length must be 16/24/32 bytes, otherwise a CryptographicException is thrown.</remarks>
        /// <param name="toEncrypt">明文（UTF-8） / The plaintext (UTF-8)</param>
        /// <param name="key">密钥（按 UTF-8 字节 16/24/32 字节） / The key (16/24/32 bytes in UTF-8)</param>
        /// <returns>Base64(IV+密文) / Base64(IV + ciphertext)</returns>
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
        /// AES-CBC 解密（IV 取密文前 16 字节），密钥取配置 SecurityUtility:AES_KEY /
        /// AES-CBC decryption (the IV is the first 16 bytes of the ciphertext); the key comes from the SecurityUtility:AES_KEY config
        /// </summary>
        /// <param name="toDecrypt">AESEncodeCBC 生成的 Base64 串 / The Base64 string produced by AESEncodeCBC</param>
        /// <returns>解密后的明文（UTF-8） / The decrypted plaintext (UTF-8)</returns>
        /// <exception cref="SuperException">配置节未配置 AES_KEY / AES_KEY is not configured</exception>
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
        /// AES-CBC 解密（IV 取密文前 16 字节） /
        /// AES-CBC decryption (the IV is the first 16 bytes of the ciphertext)
        /// </summary>
        /// <remarks>密钥按 UTF-8 字节数计须为 16/24/32 字节；密文长度不大于 IV 长度时抛 <see cref="SuperException"/>。The key's UTF-8 byte length must be 16/24/32 bytes; ciphertext at most IV-length throws <see cref="SuperException"/>.</remarks>
        /// <param name="toDecrypt">AESEncodeCBC 生成的 Base64 串 / The Base64 string produced by AESEncodeCBC</param>
        /// <param name="key">密钥（按 UTF-8 字节 16/24/32 字节） / The key (16/24/32 bytes in UTF-8)</param>
        /// <returns>解密后的明文（UTF-8） / The decrypted plaintext (UTF-8)</returns>
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
        /// Base64 编码（失败时静默返回原文，不抛异常） /
        /// Base64 encoding (silently returns the original string on failure instead of throwing)
        /// </summary>
        /// <param name="encode">编码方式 / The text encoding</param>
        /// <param name="source">待编码的明文 / The text to encode</param>
        /// <returns>Base64 字符串，失败时为原文 / The Base64 string, or the original on failure</returns>
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
        /// Base64 编码（UTF-8 编码方式；失败时静默返回原文） /
        /// Base64 encoding (UTF-8; silently returns the original string on failure)
        /// </summary>
        /// <param name="source">待编码的明文 / The text to encode</param>
        /// <returns>Base64 字符串，失败时为原文 / The Base64 string, or the original on failure</returns>
        public static string EncodeBase64(string source)
        {
            return Base64Encode(source, Encoding.UTF8);
        }

        /// <summary>
        /// Base64 解码（失败时静默返回原文，不抛异常） /
        /// Base64 decoding (silently returns the input string on failure instead of throwing)
        /// </summary>
        /// <remarks>输入为非法 Base64 串时返回原文。An invalid Base64 input returns the original string.</remarks>
        /// <param name="encode">解码采用的编码方式，须与编码时一致 / The text encoding, must match the one used to encode</param>
        /// <param name="result">待解码的 Base64 串 / The Base64 string to decode</param>
        /// <returns>解码后的字符串，失败时为原文 / The decoded string, or the input on failure</returns>
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
        /// Base64 解码（UTF-8 编码方式；失败时静默返回原文） /
        /// Base64 decoding (UTF-8; silently returns the input string on failure)
        /// </summary>
        /// <param name="result">待解码的 Base64 串 / The Base64 string to decode</param>
        /// <returns>解码后的字符串，失败时为原文 / The decoded string, or the input on failure</returns>
        public static string DecodeBase64(string result)
        {
            return DecodeBase64(result, Encoding.UTF8);
        }

        #endregion

    }
}
