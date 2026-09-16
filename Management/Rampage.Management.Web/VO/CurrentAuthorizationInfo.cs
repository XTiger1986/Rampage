using Microsoft.AspNetCore.Mvc.Controllers;
using Rampage.Localization;
using Rampage.Management.Dto;
using Rampage.Management.Permission;
using System;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 
    /// </summary>
    public class CurrentAuthorizationInfo
    {

        public AdminDto? CurrentAdmin { get; set; }

        public List<PermissionActionInfo>? AuthorizedActionList { get; set; }

        public List<ManageMenuDto>? AuthorizedMenuList { get; set; }

        public ControllerActionDescriptor? ActionDescriptor { get; set; }

        public ManageMenuDto? CurrentMenu { get; set; }

        public List<MenuBreadCrumb>? MenuBreadCrumbList { get; set; } 

        public PermissionActionInfo? CurrentAction { get; set; }

        public LocalizationModule? GlobalLanguage
        {
            get;
            set;
        }

        public LocalizationModule? PermissionLanguage
        {
            get;
            set;
        }

    }
}
