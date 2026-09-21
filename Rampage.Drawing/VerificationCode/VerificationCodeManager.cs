using System;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码管理器（工厂入口：按类型编号创建 001–010 各验证码并生成图片） /
    /// Verification code manager (factory entry that creates the 001–010 generators and renders their images).
    /// </summary>
    /// <remarks>
    ///  Author:         楊紹(Michael Zhou)
    ///  Description:    验证码管理器，用于创建各种验证码
    ///
    ///  History:        2015/4/28 17:18:23 创建
    ///
    ///  备注：001–009 各有专用快捷方法；VerificationCode010 无专用快捷方法，
    ///  需通过泛型 CreateVerificationCodeImage&lt;T&gt; 或随机入口调用。
    /// </remarks>
    public class VerificationCodeManager
    {

        /// <summary>
        /// 创建指定类型验证码的新实例 / Create a new instance of the specified verification code type.
        /// </summary>
        /// <typeparam name="T">验证码类型，须有无参构造函数 / The verification code type; must have a parameterless constructor.</typeparam>
        /// <returns>新创建的验证码实例 / The newly created instance.</returns>
        public static T CreateVerificationCode<T>() where T : VerificationCodeBase, new()
        {
            return new T();
        }


        /// <summary>
        /// 全部可用验证码类型编号（1–10 对应 VerificationCode001–VerificationCode010） /
        /// All supported verification code type ids (1–10 map to VerificationCode001–VerificationCode010).
        /// </summary>
        public static readonly int[] AllCodeTypes = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        /// <summary>
        /// 创建指定泛型类型的验证码并生成图片 / Create a verification code of the given type and render its image.
        /// </summary>
        /// <typeparam name="T">验证码类型，须有无参构造函数 / The verification code type; must have a parameterless constructor.</typeparam>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[]? CreateVerificationCodeImage<T>(out string? resultCode) where T : VerificationCodeBase, new()
        {
            byte[]? retByte = CreateVerificationCode<T>().CreateImage(out resultCode);
            return retByte;
        }

        /// <summary>
        /// 随机选取一种验证码样式并生成图片 / Create a verification code image with a randomly chosen style.
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[]? CreateVerificationCodeImage(out string? resultCode)
        {
            return CreateVerificationCodeImage(null, out resultCode);
        }
        /// <summary>
        /// 随机从指定的类型中创建一个验证码实例
        /// </summary>
        /// <param name="codeTypes"></param>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[]? CreateVerificationCodeImage(int[]? codeTypes, out string? resultCode)
        {
            if(codeTypes == null || codeTypes.Length == 0)
            {
                codeTypes = new int[AllCodeTypes.Length];
                Array.Copy(AllCodeTypes, codeTypes, AllCodeTypes.Length);
            }

            resultCode = null;
            byte[]? retByte = null;
            int count = codeTypes.Length;
            int index = new Random(Guid.NewGuid().GetHashCode()).Next(0, count);
            int codeType = codeTypes[index];
            switch (codeType)
            {
                case 1:
                    retByte = CreateVerificationCode<VerificationCode001>().CreateImage(out resultCode);
                    break;
                case 2:
                    retByte = CreateVerificationCode<VerificationCode002>().CreateImage(out resultCode);
                    break;
                case 3:
                    retByte = CreateVerificationCode<VerificationCode003>().CreateImage(out resultCode);
                    break;
                case 4:
                    retByte = CreateVerificationCode<VerificationCode004>().CreateImage(out resultCode);
                    break;
                case 5:
                    retByte = CreateVerificationCode<VerificationCode005>().CreateImage(out resultCode);
                    break;
                case 6:
                    retByte = CreateVerificationCode<VerificationCode006>().CreateImage(out resultCode);
                    break;
                case 7:
                    retByte = CreateVerificationCode<VerificationCode007>().CreateImage(out resultCode);
                    break;
                case 8:
                    retByte = CreateVerificationCode<VerificationCode008>().CreateImage(out resultCode);
                    break;
                case 9:
                    retByte = CreateVerificationCode<VerificationCode009>().CreateImage(out resultCode);
                    break;
                case 10:
                    retByte = CreateVerificationCode<VerificationCode010>().CreateImage(out resultCode);
                    break;
            }
            return retByte;
        }

        /// <summary>
        /// 创建 001 号验证码图片（线条干扰·蓝色） / Create a style-001 code image (blue, line interference).
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateVerificationCodeImage001(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode001>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 创建 002 号验证码图片（噪点干扰·蓝色·纯字母） / Create a style-002 code image (blue, dot interference, letters only).
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateVerificationCodeImage002(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode002>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 创建 003 号验证码图片（线条+噪点干扰·蓝色） / Create a style-003 code image (blue, line + dot interference).
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateVerificationCodeImage003(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode003>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 创建 004 号验证码图片（噪点干扰·正弦扭曲） / Create a style-004 code image (dot interference with sine-wave distortion).
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateVerificationCodeImage004(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode004>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 创建 005 号验证码图片（常用汉字·蓝色） / Create a style-005 code image (common Chinese characters, blue).
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateVerificationCodeImage005(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode005>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 创建 006 号验证码图片（小学加减算术·答案即验证码） / Create a style-006 code image (primary-school addition/subtraction; the answer is the code).
        /// </summary>
        /// <param name="resultCode">输出：算式的计算结果 / Output: the arithmetic result.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateVerificationCodeImage006(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode006>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 创建 007 号验证码图片（噪点干扰·彩色·逐字符随机色） / Create a style-007 code image (colored dot interference, per-character random color).
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateVerificationCodeImage007(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode007>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 创建 008 号验证码图片（线条干扰·彩色·逐字符随机色） / Create a style-008 code image (colored line interference, per-character random color).
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateVerificationCodeImage008(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode008>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 创建 009 号验证码图片（常用汉字·彩色） / Create a style-009 code image (common Chinese characters, colored).
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串 / Output: the string the user must enter.</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateVerificationCodeImage009(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode009>().CreateImage(out resultCode);
        }
       
    }
}
