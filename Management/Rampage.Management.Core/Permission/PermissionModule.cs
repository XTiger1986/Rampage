using System;

namespace Rampage.Management.Permission
{
    /// <summary>
    /// 标识权限系统中的模块（应用于控制器类）
    /// Identify the module in the permission system (applied to controller classes)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PermissionModule : Attribute
    {

        private bool _ignore = false;
        /// <summary>忽略该模块，不加入权限系统验证 / Ignore this module; exclude it from permission system verification</summary>
        public bool Ignore
        {
            get { return _ignore; }
            set { _ignore = value; }
        }

    }
}
