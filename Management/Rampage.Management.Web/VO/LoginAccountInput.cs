using Rampage.Validation;
using System;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 登录账号输入
    /// </summary>
    public class LoginAccountInput
    {
        /// <summary>
        /// 
        /// </summary>
        [NotWhiteSpaceString(MessageTemplate = "登录账号不能为空")]
        [StringLength(Min = 2, Max = 80, MessageTemplate = "登录账号长度必须在{1}~{2}位之间")]
        public string? Account
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [NotWhiteSpaceString(MessageTemplate = "登录密码不能为空")]
        [StringLength(Min = 5, Max = 20, MessageTemplate = "登录密码长度必须在{1}~{2}位之间")]
        public string? Password
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsRemember
        {
            get;
            set;
        }

    }
}
