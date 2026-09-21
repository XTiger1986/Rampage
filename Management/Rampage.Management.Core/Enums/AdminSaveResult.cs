using Rampage.Enums;
using System;

namespace Rampage.Management
{

    /// <summary>保存管理员结果类型 / Save administrator result types</summary>
    public enum AdminSaveResult : int
    {
        /// <summary>用户名已存在（90001） / The username already exists</summary>
        [EnumFieldAttribute("The username already exists")]
        ExistsUsername = 90001,
        /// <summary>邮箱已存在（90002） / The email already exists</summary>
        [EnumFieldAttribute("Email already exists")]
        ExistsEmail = 90002,
        /// <summary>手机号已存在（90003） / The phone number already exists</summary>
        [EnumFieldAttribute("The phone number already exists")]
        ExistsMobile = 90003,

    }

}
