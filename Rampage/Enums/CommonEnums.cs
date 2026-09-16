using System;

namespace Rampage.Enums
{

    /// <summary>
    /// 
    /// </summary>
    public enum CommonStatus : short
    {
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("Enabled")]
        Enabled = 1,

        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("Disabled")]
        Disabled = 0
    }

    /// <summary>
    /// 
    /// </summary>
    public enum UserGender : short
    {
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("Unknown")]
        Unknown = 0,
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("Male")]
        Male = 1,
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("Female")]
        Female = 2
    }

    /// <summary>
    /// 
    /// </summary>
    public enum YesOrNoStatus : short
    {
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("Yes")]
        Yes = 1,

        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("No")]
        No = 0
    }

    /// <summary>
    /// 
    /// </summary>
    public enum RightOrWrongStatus : short
    {
        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("Correct")]
        Correct = 1,

        /// <summary>
        /// 
        /// </summary>
        [EnumFieldAttribute("Incorrect")]
        Incorrect = 0
    }

}
