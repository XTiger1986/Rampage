using Rampage.Enums;
using System;

namespace Rampage.Validation
{

    /// <summary>
    /// 验证结果代码 / Validation result codes
    /// </summary>
    public enum ValidationResultCode
    {
        /// <summary>
        /// 验证通过 / Validation passed
        /// </summary>
        [EnumFieldAttribute("验证通过")]
        Success = 1,
        /// <summary>
        /// 验证通过，但存在问题（警告）/ Validation passed with warnings
        /// </summary>
        [EnumFieldAttribute("验证通过，但存在问题")]
        Warning = 2,
        /// <summary>
        /// 验证不通过 / Validation failed
        /// </summary>
        [EnumFieldAttribute("验证不通过")]
        Failure = 3
    }

}
