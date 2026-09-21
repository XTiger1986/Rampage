using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rampage.AppLog;
using Rampage.Management.Configuration;
using Rampage.Management.Dto;
using Rampage.Management.VO;
using Rampage.Model;
using Rampage.Validation;
using System;

namespace Rampage.Management.Web.Api
{

    /// <summary>
    /// 登录通行证 API 控制器（路由 api/passport）：提供管理员登录与登出，整控制器允许匿名访问
    /// Passport API controller (route api/passport): provides admin sign-in and sign-out; the whole controller allows anonymous access
    /// </summary>
    [Route("api/passport")]
    [AllowAnonymousAttribute]
    public class PassportApiController : SuperManagementApiController
    {

        /// <summary>
        /// 管理员登录 / Signs in an administrator
        /// </summary>
        /// <param name="input">登录输入（账号、密码、是否记住登录）/ The login input (account, password, remember-me)</param>
        /// <returns>登录结果，成功时 Data 为管理员信息 / The login result; Data holds the admin info on success</returns>
        /// <remarks>勾选记住登录时，登录有效期按 ManagementConfig.RememberDays 天计算。异常统一捕获、记日志后以失败结果返回。</remarks>
        [HttpPost("login")]
        public R QueryLogin([ValidAttribute][FromBody] LoginAccountInput input)
        {
            try
            {
                string account = input.Account!;
                string password = input.Password!;
                bool isRemember = input.IsRemember;
                double? rememberDays = isRemember ? ManagementConfig.RememberDays : null;

                R<AdminDto> operationResult = AdminService.Login(account, password, rememberDays);
                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R<AdminDto>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 管理员登出 / Signs out the current administrator
        /// </summary>
        /// <returns>操作结果 / The operation result</returns>
        [HttpPost("logout")]
        public R QueryLogout()
        {
            try
            {
                return AdminService.Logout();
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

    }
}
