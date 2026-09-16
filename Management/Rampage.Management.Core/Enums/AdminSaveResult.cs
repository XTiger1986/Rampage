using Rampage.Enums;
using System;

namespace Rampage.Management
{

    /// <summary>
    /// Save Administrator Result Types
    /// 保存管理员结果
    /// </summary>
    public enum AdminSaveResult : int
    {
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("The username already exists")]
        ExistsUsername = 90001,
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("Email already exists")]
        ExistsEmail = 90002,
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("The phone number already exists")]
        ExistsMobile = 90003,

    }

}
