using Rampage.Enums;
using System;

namespace Rampage.Management
{
    /// <summary>管理功能类型 / Management function types</summary>
    public enum PermissionActionType
    {
        /// <summary>页面 / Page</summary>
        [EnumFieldAttribute("Page")]
        Page = 1,

        /// <summary>查询 / Query</summary>
        [EnumFieldAttribute("Query")]
        Query = 2,

        /// <summary>操作 / Operation</summary>
        [EnumFieldAttribute("Operation")]
        Operation = 3,

        /// <summary>删除 / Deletion</summary>
        [EnumFieldAttribute("Deletion")]
        Delete = 4
    }

}
