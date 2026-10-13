using Rampage.Management.Security;
using Rampage.Validation;
using System;
using System.Collections.Generic;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 登录账号输入 / Login account input
    /// </summary>
    public class LoginAccountInput
    {
        /// <summary>
        /// 登录账号 / The login account
        /// </summary>
        [NotWhiteSpaceString(MessageTemplate = "登录账号不能为空")]
        [StringLength(Min = 2, Max = 80, MessageTemplate = "登录账号长度必须在{1}~{2}位之间")]
        public string? Account
        {
            get;
            set;
        }

        /// <summary>
        /// 登录密码 / The login password
        /// </summary>
        [NotWhiteSpaceString(MessageTemplate = "登录密码不能为空")]
        [StringLength(Min = 5, Max = 20, MessageTemplate = "登录密码长度必须在{1}~{2}位之间")]
        public string? Password
        {
            get;
            set;
        }

        /// <summary>
        /// 是否记住登录（勾选后登录有效期按 ManagementConfig.RememberDays 天计算）
        /// Whether to remember the login (when checked, the login validity spans ManagementConfig.RememberDays days)
        /// </summary>
        public bool IsRemember
        {
            get;
            set;
        }

        /// <summary>
        /// 登录验证码令牌（启用登录验证码时必填，来自 /api/passport/captcha 响应的 Token）
        /// The login captcha token (required when the login captcha is enabled; the Token from the /api/passport/captcha response)
        /// </summary>
        [StringLength(Max = 64, MessageTemplate = "验证码令牌非法")]
        public string? CaptchaToken
        {
            get;
            set;
        }

        /// <summary>
        /// 图形验证码答案（input 交互模式填写；点选模式不使用）
        /// The graphic captcha answer (for the "input" interaction mode; unused in click mode)
        /// </summary>
        [StringLength(Max = 32, MessageTemplate = "验证码答案长度非法")]
        public string? CaptchaCode
        {
            get;
            set;
        }

        /// <summary>
        /// 点选验证码点击坐标序列（click 交互模式，按点击顺序、图片自然像素；click 模式下必填）
        /// The click captcha points (for the "click" interaction mode; in click order and natural image pixels; required in click mode)
        /// </summary>
        /// <remarks>不加验证特性：校验器只扫顶层标量属性，集合类型由控制器按交互模式判空 / No validation attributes: the validator only scans top-level scalar properties, and the controller checks emptiness per interaction mode</remarks>
        public List<CaptchaPoint>? CaptchaPoints
        {
            get;
            set;
        }

    }
}
