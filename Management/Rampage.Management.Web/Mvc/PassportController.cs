using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rampage.Management.Security;
using Rampage.Web.Utilities;
using System;

namespace Rampage.Management.Mvc
{
    /// <summary>
    /// 登录通行证控制器：渲染登录页，允许匿名访问
    /// Passport controller: renders the sign-in page; anonymous access is allowed
    /// </summary>
    [AllowAnonymousAttribute]
    public class PassportController : SuperManagementController
    {


        /// <summary>
        /// 登录页 / The sign-in page
        /// </summary>
        /// <returns>登录页视图 / The sign-in view</returns>
        /// <remarks>ReturnUrl 经 WebTool.IsLocalUrl 校验，仅接受站内本地地址（防开放跳转），非法或缺失时回退到 "/"。
        /// 另按 LoginCaptchaOptions 把 CaptchaEnabled 传给视图，决定是否渲染验证码区块（配置热更新，刷新页面即生效）。</remarks>
        public IActionResult SignIn()
        {
            string? returnUrl = Get<string>("ReturnUrl");
            if (string.IsNullOrEmpty(returnUrl) || !WebTool.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.CaptchaEnabled = LoginCaptchaOptions.Load().Enabled;
            return VW();
        }

    }
}
