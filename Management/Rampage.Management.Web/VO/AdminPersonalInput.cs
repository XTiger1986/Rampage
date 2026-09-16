using Rampage.Enums;
using Rampage.Validation;
using System;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 
    /// </summary>
    public class AdminPersonalInput
    {

        /// <summary>
        /// 用户名
        /// </summary>
        [NotWhiteSpaceString(MessageTemplate = "用户名不能为空")]
        public string? Username { get; set; }

        /// <summary>
        /// 真实姓名
        /// </summary>
        [StringLength(Max = 30, MessageTemplate = "真实姓名不能超过{1}个字符")]
        public string? TrueName { get; set; }

        /// <summary>
        /// 手机
        /// </summary>
        [MobileFormat(MessageTemplate = "手机号格式不正确")]
        public string? Mobile { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [EmailFormat(MessageTemplate = "邮箱格式不正确")]
        public string? Email { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public UserGender Gender { get; set; }

    }
}
