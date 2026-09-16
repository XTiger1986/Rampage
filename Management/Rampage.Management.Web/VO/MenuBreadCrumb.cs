using Rampage.Management.Permission;
using System;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 
    /// </summary>
    public class MenuBreadCrumb
    {

        public int Id { get; set; }

        public int ParentId { get; set; }

        public string? Title { get; set; }

        public string? Url { get; set; }

        public PermissionActionInfo? ActionInfo { get; set; }

    }
}
