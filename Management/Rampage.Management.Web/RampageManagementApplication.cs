using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rampage.Management.Configuration;
using Rampage.Management.Mvc;
using Rampage.Management.Web.Api;
using Rampage.Web;

namespace Rampage.Management.Web
{
    /// <summary>
    /// 
    /// </summary>
    public class RampageManagementApplication : RampageWebApplication
    {

        public RampageManagementApplication() : base()
        {
            ApplicationInitialized += (app, provider) =>
            {
                ManagementConfig.ActionViewBaseControllerType = typeof(Controller);
                ManagementConfig.WebApiBaseControllerType = typeof(ControllerBase);
                ManagementConfig.AssignableControllerTypes = new List<Type>
                {
                    typeof(SuperManagementController),
                    typeof(SuperManagementApiController)
                };
                ManagementConfig.DefinedAnonymousFilterType = typeof(AllowAnonymousAttribute);
                ManagementConfig.DefinedLoggedInFilterType = typeof(PermissionLoggedInFilter);
            };
        }

    }
}
