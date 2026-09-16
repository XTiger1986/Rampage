using System;

namespace Rampage.Management.Permission
{
    /// <summary>
    /// Identify the Controller in the permission system
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PermissionModule : Attribute
    {

        private bool _ignore = false;
        /// <summary>
        /// Ignore this module and do not join the permission system verification
        /// </summary>
        public bool Ignore
        {
            get { return _ignore; }
            set { _ignore = value; }
        }

    }
}
