using System;

namespace Rampage.Management.Permission
{

    /// <summary>
    /// 标识权限系统中的视图（应用于控制器方法）
    /// Identify the view (action) in the permission system (applied to controller methods)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PermissionAction : Attribute
    {

        private bool _ignore = false;
        /// <summary>忽略该视图，不加入权限系统验证 / Ignore this action; exclude it from permission system validation</summary>
        public bool Ignore
        {
            get { return _ignore; }
            set { _ignore = value; }
        }

        /// <summary>权限资源组标识（为空时按 区域_控制器_视图 生成） / Permission resource group identifier (generated as area_controller_action when empty)</summary>
        public string? Resource
        {
            get;
            set;
        }

        /// <summary>功能类型（页面/查询/操作/删除） / The action type (page/query/operation/delete)</summary>
        public PermissionActionType Type
        {
            get;
            set;
        }

        /// <summary>功能标题 / Action title</summary>
        public string? Title
        {
            get; 
            set;
        }

        /// <summary>多语言标题的本地化键（为空时按 命名空间.类型.方法名 生成） / Localizer key for the multilingual title (generated as namespace.type.method when empty)</summary>
        public string? LocalizerKey
        {
            get; set;
        }

        /// <summary>功能说明 / Action description</summary>
        public string? Description
        {
            get; 
            set;
        }

        /// <summary>排序值（值越小越优先） / Sort order (smaller = higher priority)</summary>
        public int SortOrder
        {
            get; 
            set;
        }

    }

}
