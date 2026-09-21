using Rampage.Enums;
using Rampage.Validation;
using System;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 管理员个人资料输入（仅允许修改自身的展示类资料字段）
    /// Admin personal profile input (only the display fields of one's own profile)
    /// </summary>
    public class AdminPersonalInput
    {

        /// <summary>
        /// 用户名 / The username
        /// </summary>
        [NotWhiteSpaceString(MessageTemplate = "用户名不能为空")]
        public string? Username { get; set; }

        /// <summary>
        /// 真实姓名 / The real name
        /// </summary>
        [StringLength(Max = 30, MessageTemplate = "真实姓名不能超过{1}个字符")]
        public string? TrueName { get; set; }

        /// <summary>
        /// 手机 / The mobile number
        /// </summary>
        [MobileFormat(MessageTemplate = "手机号格式不正确")]
        public string? Mobile { get; set; }

        /// <summary>
        /// 邮箱 / The email
        /// </summary>
        [EmailFormat(MessageTemplate = "邮箱格式不正确")]
        public string? Email { get; set; }

        /// <summary>
        /// 头像 / The avatar
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 性别 / The gender
        /// </summary>
        public UserGender Gender { get; set; }

    }
}
