using Rampage.DependencyInjection;
using Rampage.Management.BLL;
using Rampage.Management.Dto;
using Rampage.Management.Web.Mvc;
using Rampage.Model;
using Rampage.Web.Mvc;
using System;

namespace Rampage.Management.Web.Api
{
    [ManagementAuthorizationFilter]
    public abstract class SuperManagementApiController : SuperApiController
    {

        /// <summary>
        /// 
        /// </summary>
        [Auto]
        protected IAdminService AdminService = default!;
        [Auto]
        protected IAdminPermissionService AdminPermissionService = default!;
        [Auto]
        protected IManageMenuService ManageMenuService = default!;

        /// <summary>
        /// 
        /// </summary>
        protected AdminDto? CurrentAdmin
        {
            get;
            private set;
        }

        protected override void Initialize()
        {
            base.Initialize();

            R<AdminDto> loginR = AdminService.GetLoginAdmin();
            if (loginR.IsSuccess)
            {
                CurrentAdmin = loginR.Data;
            }
        }


    }
}
