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
    ///  点选式验证码（点选文字 / 点选单词）为独立契约（答案为坐标范围而非字符串），不进入
    ///  AllCodeTypes 随机池，分别经 CreateClickVerificationCodeImage /
    ///  CreateWordClickVerificationCodeImage 调用。
    ///  Click captchas (characters / words) use a separate contract (regions instead of a string answer),
    ///  never join the AllCodeTypes random pool, and are created via the dedicated
    ///  CreateClickVerificationCodeImage / CreateWordClickVerificationCodeImage.
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

        /// <summary>
        /// 创建点选验证码图片（依次点击汉字；独立契约，不进入 AllCodeTypes 随机池） /
        /// Create a click captcha image (click the characters in order; separate contract, never in the AllCodeTypes pool).
        /// </summary>
        /// <remarks>
        /// 使用方式：图片字节直接输出给前端展示；result.ToJson() 序列化后存入原有验证码存储位
        /// （替代普通验证码的字符串答案）；校验时 ClickCodeResult.FromJson(json).Verify(前端点数组)。
        /// Usage: stream the bytes to the frontend; store result.ToJson() where the plain code string used to live;
        /// validate via ClickCodeResult.FromJson(json).Verify(frontendPoints).
        /// </remarks>
        /// <param name="result">输出：验证结果（目标字串 + 各目标字可点击坐标范围，按提示顺序） /
        /// Output: the verification result (the target code plus each target's clickable region, in prompt order).</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateClickVerificationCodeImage(out ClickCodeResult result)
        {
            return new ClickVerificationCode().CreateImage(out result);
        }

        /// <summary>
        /// 创建英文单词点选验证码图片（依次点击提示单词；独立契约，不进入 AllCodeTypes 随机池） /
        /// Create an English word click captcha image (click the prompted words in order; separate contract, never in the AllCodeTypes pool).
        /// </summary>
        /// <remarks>
        /// 默认在“2 目标词+2 干扰词”与“3 目标词+1 干扰词”间随机取词组合；词源自内置常用词表，
        /// 可经 ClickVerificationCodeOptions.EnglishWordPool 覆盖。存储与校验方式与中文点选一致。
        /// The count mix randomly alternates between "2 targets + 2 distractors" and "3 targets + 1 distractor";
        /// words come from the built-in pool, overridable via ClickVerificationCodeOptions.EnglishWordPool.
        /// Storage and validation match the character mode.
        /// </remarks>
        /// <param name="result">输出：验证结果（目标词串 + 各目标词可点击坐标范围，按提示顺序） /
        /// Output: the verification result (the target code plus each target word's clickable region, in prompt order).</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public static byte[] CreateWordClickVerificationCodeImage(out ClickCodeResult result)
        {
            return new WordClickVerificationCode().CreateImage(out result);
        }

    }
}
