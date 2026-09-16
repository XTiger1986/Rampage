using System;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码管理器
    /// </summary>
    /// <remarks>
    ///  Author:         楊紹(Michael Zhou)
    ///  Description:    验证码管理器，用于创建各种验证码
    ///                  
    ///  History:        2015/4/28 17:18:23 创建
    ///                  
    /// </remarks>
    public class VerificationCodeManager
    {

        /// <summary>
        /// 创建一个验证码实例
        /// </summary>
        /// <returns></returns>
        public static T CreateVerificationCode<T>() where T : VerificationCodeBase, new()
        {
            return new T();
        }


        /// <summary>
        /// 所有验证码类型
        /// </summary>
        public static readonly int[] AllCodeTypes = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        /// <summary>
        /// 创建一个自定义类型的验证码实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[]? CreateVerificationCodeImage<T>(out string? resultCode) where T : VerificationCodeBase, new()
        {
            byte[]? retByte = CreateVerificationCode<T>().CreateImage(out resultCode);
            return retByte;
        }

        /// <summary>
        /// 随机创建一个验证码实例
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
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
            int index = new Random(Guid.NewGuid().GetHashCode()).Next(0, count - 1);
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
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[] CreateVerificationCodeImage001(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode001>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[] CreateVerificationCodeImage002(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode002>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[] CreateVerificationCodeImage003(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode003>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[] CreateVerificationCodeImage004(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode004>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[] CreateVerificationCodeImage005(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode005>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[] CreateVerificationCodeImage006(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode006>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[] CreateVerificationCodeImage007(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode007>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[] CreateVerificationCodeImage008(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode008>().CreateImage(out resultCode);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public static byte[] CreateVerificationCodeImage009(out string resultCode)
        {
            return CreateVerificationCode<VerificationCode009>().CreateImage(out resultCode);
        }
       
    }
}
