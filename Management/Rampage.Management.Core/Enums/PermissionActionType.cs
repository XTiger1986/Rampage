using Rampage.Enums;
using System;

namespace Rampage.Management
{
    /// <summary>
    /// Management Function Types
    /// 管理功能类型
    /// </summary>
    public enum PermissionActionType
    {
        [EnumFieldAttribute("Page")]
        Page = 1,

        [EnumFieldAttribute("Query")]
        Query = 2,

        [EnumFieldAttribute("Operation")]
        Operation = 3,

        [EnumFieldAttribute("Deletion")]
        Delete = 4
    }

}
