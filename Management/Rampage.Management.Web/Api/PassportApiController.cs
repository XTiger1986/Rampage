using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rampage.AppLog;
using Rampage.Management.Configuration;
using Rampage.Management.Dto;
using Rampage.Management.Security;
using Rampage.Management.VO;
using Rampage.Model;
using Rampage.Validation;
using System;

namespace Rampage.Management.Web.Api
{

    /// <summary>
    /// 登录通行证 API 控制器（路由 api/passport）：提供管理员登录、登出与登录验证码，整控制器允许匿名访问
    /// Passport API controller (route api/passport): provides admin sign-in, sign-out and the login captcha; the whole controller allows anonymous access
    /// </summary>
    [Route("api/passport")]
    [AllowAnonymousAttribute]
    public class PassportApiController : SuperManagementApiController
    {

        /// <summary>
        /// 登录验证码服务：优先取 DI 注册实现（集群可注册分布式版），否则用进程内默认实例；
        /// 勿改用 [Auto] 注入——框架会将其注册为 Scoped 而丢弃进程内存储
        /// The login captcha service: prefer the DI-registered implementation (register a distributed one for clusters), else the in-process default;
        /// do not switch to [Auto] injection — the framework would register it as Scoped and discard the in-process store
        /// </summary>
        protected virtual ILoginCaptchaService LoginCaptchaService
        {
            get
            {
                return ServiceLocator?.TryGet<ILoginCaptchaService>() ?? MemoryLoginCaptchaService.Default;
            }
        }

        /// <summary>
        /// 获取登录验证码图片 / Gets a login captcha image
        /// </summary>
        /// <returns>登录验证码响应（令牌 + Base64 + 交互模式 + 提示文案）；未启用时返回失败 / The captcha payload (token + base64 + mode + tip); a failure when disabled</returns>
        /// <remarks>按 LoginCaptchaOptions.CodeTypes 随机选型（1–10 图形、11 点选文字、12 点选单词）；令牌一次性，核验即消费。</remarks>
        [HttpGet("captcha")]
        public R QueryCaptcha()
        {
            try
            {
                if (!LoginCaptchaOptions.Load().Enabled)
                {
                    return R.Failure("验证码未启用");
                }
                return R.Success(LoginCaptchaService.Generate());
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 管理员登录 / Signs in an administrator
        /// </summary>
        /// <param name="input">登录输入（账号、密码、是否记住登录、验证码三件套）/ The login input (account, password, remember-me, captcha fields)</param>
        /// <returns>登录结果，成功时 Data 为管理员信息 / The login result; Data holds the admin info on success</returns>
        /// <remarks>勾选记住登录时，登录有效期按 ManagementConfig.RememberDays 天计算。启用登录验证码时先核验并一次性消费验证码，
        /// 验证码失败不进入密码校验、也不消耗防爆破计数。异常统一捕获、记日志后以失败结果返回。</remarks>
        [HttpPost("login")]
        public R QueryLogin([ValidAttribute][FromBody] LoginAccountInput input)
        {
            try
            {
                // 启用登录验证码时先核验（一次性消费）；垃圾坐标防御上限 16 点
                // When the login captcha is enabled, verify (one-shot) first; cap junk click payloads at 16 points
                if (LoginCaptchaOptions.Load().Enabled)
                {
                    if (input.CaptchaPoints != null && input.CaptchaPoints.Count > 16)
                    {
                        return R.Create(ResultCode.IncorrectVerificationCode);
                    }
                    R captchaResult = LoginCaptchaService.Verify(input.CaptchaToken, input.CaptchaCode, input.CaptchaPoints);
                    if (!captchaResult.IsSuccess)
                    {
                        return captchaResult;
                    }
                }

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
