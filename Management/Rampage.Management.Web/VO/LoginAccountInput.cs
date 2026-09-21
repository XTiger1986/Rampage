using Rampage.Validation;
using System;

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

    }
}
