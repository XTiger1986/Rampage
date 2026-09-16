using System;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码基类
    /// </summary>
    /// <remarks>
    ///  Author:         楊紹(Michael Zhou)
    ///  Description:    验证码基类，所有验证码类型均继承此类
    ///                  
    ///  History:        2015/4/28 17:20:13 创建
    ///                  
    /// </remarks>
    public abstract class VerificationCodeBase
    {
        /// <summary>
        /// 
        /// </summary>
        public VerificationCodeBase()
        {
        }

        /// <summary>
        /// 类型
        /// </summary>
        public string Type
        {
            get { return this.GetType().Name; }
        }

        /// <summary>
        /// 名称
        /// </summary>
        public abstract string Name 
        { 
            get; 
        }

        /// <summary>
        /// 填写验证码提示 ：如"请输入计算结果"
        /// </summary>
        public virtual string Tip 
        { 
            get { return "请输入图片中的字符"; } 
        }

        /// <summary>
        /// 生成图片
        /// </summary>
        /// <param name="resultCode">用户必须填的字符串</param>
        /// <returns></returns>
        public abstract byte[] CreateImage(out string resultCode);


        /// <summary>
        /// 是否开启输入法
        /// </summary>
        public virtual bool DisableIme
        {
            get
            {
                return true;
            }
        }
    }
}
