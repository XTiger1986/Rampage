using System;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码基类（模板方法：由子类实现 CreateImage 产出 PNG 图片与答案） /
    /// Base class for verification codes (template method: subclasses implement CreateImage to produce PNG bytes and the answer).
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
        /// 初始化验证码基类 / Initializes the base verification code class.
        /// </summary>
        public VerificationCodeBase()
        {
        }

        /// <summary>
        /// 获取验证码类型名（返回具体实现类的类名，如 VerificationCode001） /
        /// Gets the type name of the concrete implementation (e.g. VerificationCode001).
        /// </summary>
        public string Type
        {
            get { return this.GetType().Name; }
        }

        /// <summary>
        /// 获取验证码样式的中文名称（用于展示） / Gets the display name of the verification code style.
        /// </summary>
        public abstract string Name 
        { 
            get; 
        }

        /// <summary>
        /// 获取验证码输入提示文案（默认“请输入图片中的字符”；算术题型重写为“输入计算结果”） /
        /// Gets the hint shown next to the input box (default "请输入图片中的字符"; arithmetic codes override it with "输入计算结果").
        /// </summary>
        public virtual string Tip 
        { 
            get { return "请输入图片中的字符"; } 
        }

        /// <summary>
        /// 生成验证码图片 / Generates the verification code image.
        /// </summary>
        /// <param name="resultCode">输出：用户必须填写的答案字符串（普通型为图片中的字符本身，算术型为计算结果） /
        /// Output: the string the user must enter (the characters shown, or the arithmetic result for math codes).</param>
        /// <returns>PNG 图片字节数组 / The PNG image bytes.</returns>
        public abstract byte[] CreateImage(out string resultCode);


        /// <summary>
        /// 获取是否禁用输入法（默认 true 禁用；中文验证码重写为 false 以允许输入法输入汉字） /
        /// Gets whether the IME should be disabled (true by default; Chinese codes override it to false so CJK input is allowed).
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
