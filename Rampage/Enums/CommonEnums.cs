using System;

namespace Rampage.Enums
{

    /// <summary>
    /// 通用启用/停用状态 / Common enabled/disabled status
    /// </summary>
    public enum CommonStatus : short
    {
        /// <summary>
        /// 启用 / Enabled
        /// </summary>
        [EnumFieldAttribute("Enabled")]
        Enabled = 1,

        /// <summary>
        /// 停用 / Disabled
        /// </summary>
        [EnumFieldAttribute("Disabled")]
        Disabled = 0
    }

    /// <summary>
    /// 用户性别 / User gender
    /// </summary>
    public enum UserGender : short
    {
        /// <summary>
        /// 未知 / Unknown
        /// </summary>
        [EnumFieldAttribute("Unknown")]
        Unknown = 0,
        /// <summary>
        /// 男 / Male
        /// </summary>
        [EnumFieldAttribute("Male")]
        Male = 1,
        /// <summary>
        /// 女 / Female
        /// </summary>
        [EnumFieldAttribute("Female")]
        Female = 2
    }

    /// <summary>
    /// 是/否状态 / Yes or no status
    /// </summary>
    public enum YesOrNoStatus : short
    {
        /// <summary>
        /// 是 / Yes
        /// </summary>
        [EnumFieldAttribute("Yes")]
        Yes = 1,

        /// <summary>
        /// 否 / No
        /// </summary>
        [EnumFieldAttribute("No")]
        No = 0
    }

    /// <summary>
    /// 对/错状态 / Right or wrong status
    /// </summary>
    public enum RightOrWrongStatus : short
    {
        /// <summary>
        /// 正确 / Correct
        /// </summary>
        [EnumFieldAttribute("Correct")]
        Correct = 1,

        /// <summary>
        /// 错误 / Incorrect
        /// </summary>
        [EnumFieldAttribute("Incorrect")]
        Incorrect = 0
    }

}
