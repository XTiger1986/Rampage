using Rampage.Validation;
using System;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 
    /// </summary>
    public class ChangePasswordInput
    {

        /// <summary>
        /// 密码
        /// </summary>
        [StringLength(Min = 5, Max = 20, Required = true, MessageTemplate = "新密码长度必须在{1}~{2}位之间")]
        public string? Password { get; set; }

        /// <summary>
        /// 原密码
        /// </summary>
        [StringLength(Min = 5, Max = 20, Required = true, MessageTemplate = "原密码长度必须在{1}~{2}位之间")]
        public string? VerifyPassword { get; set; }

    }
}
