using Rampage.Enums;
using System;

namespace Rampage.Validation
{

    /// <summary>
    /// 
    /// </summary>
    public enum ValidationResultCode
    {
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("验证通过")]
        Success = 1,
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("验证通过，但存在问题")]
        Warning = 2,
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("验证不通过")]
        Failure = 3
    }

}
