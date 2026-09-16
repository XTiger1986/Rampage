using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rampage.Web.Utilities;
using System;

namespace Rampage.Management.Mvc
{
    /// <summary>
    /// 
    /// </summary>
    [AllowAnonymousAttribute]
    public class PassportController : SuperManagementController
    {


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IActionResult SignIn()
        {
            string? returnUrl = Get<string>("ReturnUrl");
            if (string.IsNullOrEmpty(returnUrl) || !WebTool.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }
            ViewBag.ReturnUrl = returnUrl;
            return VW();
        }

    }
}
