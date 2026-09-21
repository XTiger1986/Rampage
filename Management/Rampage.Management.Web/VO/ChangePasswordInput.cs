using Rampage.Validation;
using System;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 修改密码输入（新密码与原密码）
    /// Change-password input (the new password and the original password)
    /// </summary>
    public class ChangePasswordInput
    {

        /// <summary>
        /// 新密码 / The new password
        /// </summary>
        [StringLength(Min = 5, Max = 20, Required = true, MessageTemplate = "新密码长度必须在{1}~{2}位之间")]
        public string? Password { get; set; }

        /// <summary>
        /// 原密码（用于校验操作者身份）/ The original password (used to verify the operator's identity)
        /// </summary>
        [StringLength(Min = 5, Max = 20, Required = true, MessageTemplate = "原密码长度必须在{1}~{2}位之间")]
        public string? VerifyPassword { get; set; }

    }
}
