using System;

namespace Rampage.Management.Permission
{

    /// <summary>
    /// Identify the View in the permission system
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PermissionAction : Attribute
    {

        private bool _ignore = false;
        /// <summary>
        /// 忽略该视图，不加入权限系统验证
        /// </summary>
        public bool Ignore
        {
            get { return _ignore; }
            set { _ignore = value; }
        }

        /// <summary>
        /// 权限资源组标识
        /// </summary>
        public string? Resource
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public PermissionActionType Type
        {
            get;
            set;
        }

        /// <summary>
        /// 功能标题
        /// </summary>
        public string? Title
        {
            get; 
            set;
        }

        public string? LocalizerKey
        {
            get; set;
        }

        /// <summary>
        /// 功能说明
        /// </summary>
        public string? Description
        {
            get; 
            set;
        }

        /// <summary>
        /// 排序值（值越小，越优先）
        /// </summary>
        public int SortOrder
        {
            get; 
            set;
        }

    }

}
