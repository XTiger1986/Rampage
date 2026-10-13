using Rampage.Model;
using System;
using System.Collections.Generic;

namespace Rampage.Management.Security
{
    /// <summary>
    /// 登录验证码点选坐标（前端点击位置换算成的图片自然像素坐标）
    /// / A login-captcha click point (a frontend click converted to natural image pixels)
    /// </summary>
    public sealed class CaptchaPoint
    {
        /// <summary>X 坐标（自然像素）/ The X coordinate (natural pixels)</summary>
        public float X
        {
            get;
            set;
        }

        /// <summary>Y 坐标（自然像素）/ The Y coordinate (natural pixels)</summary>
        public float Y
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 登录验证码图片响应（一次性令牌 + Base64 图片 + 交互模式 + 提示文案）
    /// / The login captcha image payload (one-time token + base64 image + interaction mode + tip text)
    /// </summary>
    public sealed class LoginCaptchaImage
    {
        /// <summary>一次性令牌，登录时随 CaptchaToken 回传 / The one-time token to submit back as CaptchaToken at login</summary>
        public string Token
        {
            get;
            set;
        } = string.Empty;

        /// <summary>PNG 图片字节的 Base64 / The base64 of the PNG image bytes</summary>
        public string Base64
        {
            get;
            set;
        } = string.Empty;

        /// <summary>交互模式："input"（输入答案）或 "click"（按序点击采点）/ Interaction mode: "input" (type the answer) or "click" (click the targets in order)</summary>
        public string Mode
        {
            get;
            set;
        } = string.Empty;

        /// <summary>提示文案（图形型来自 VerificationCodeBase.Tip，如算术型"输入计算结果"；点选型为固定文案）/ The tip text (graphic styles use VerificationCodeBase.Tip, e.g. "输入计算结果" for arithmetic; click styles use a fixed text)</summary>
        public string Tip
        {
            get;
            set;
        } = string.Empty;
    }

    /// <summary>
    /// 登录验证码服务：按配置生成验证码图片并暂存答案，核验时一次性消费。
    /// 进程内默认实现见 <see cref="MemoryLoginCaptchaService"/>；集群部署请在扩展库中提供分布式实现并注册到 DI。
    /// / Login captcha service: generates captcha images per the configuration and stores the answers, then verifies and consumes them one-shot.
    /// In-process default implementation: <see cref="MemoryLoginCaptchaService"/>; provide a distributed implementation in an extension library and register it in DI for clustered deployments.
    /// </summary>
    /// <remarks>
    /// 【禁止用 [Auto] 引用本接口】框架把被 [Auto] 成员引用的实现注册为 Scoped，每请求新实例会丢弃
    /// MemoryLoginCaptchaService 的进程内存储。取用方式照抄 ILoginAttemptTracker 先例：
    /// ServiceLocator?.TryGet&lt;ILoginCaptchaService&gt;() ?? MemoryLoginCaptchaService.Default。
    /// [Do NOT reference this interface via [Auto]] the framework registers implementations referenced from [Auto]
    /// members as Scoped, and a per-request instance would discard MemoryLoginCaptchaService's in-process store.
    /// Follow the ILoginAttemptTracker precedent:
    /// ServiceLocator?.TryGet&lt;ILoginCaptchaService&gt;() ?? MemoryLoginCaptchaService.Default.
    /// </remarks>
    public interface ILoginCaptchaService
    {
        /// <summary>
        /// 生成一张登录验证码（按 LoginCaptchaOptions 的类型集合随机选型：1–10 图形、11 点选文字、12 点选单词）
        /// / Generates a login captcha (a random type drawn from the LoginCaptchaOptions set: 1–10 graphic, 11 click-text, 12 click-word)
        /// </summary>
        /// <returns>验证码图片响应 / The captcha image payload</returns>
        /// <exception cref="InvalidOperationException">底层生成失败（图片或答案为空）/ The underlying generation failed (empty image or answer)</exception>
        LoginCaptchaImage Generate();

        /// <summary>
        /// 核验并一次性消费验证码（无论对错都消费，防止凭旧令牌反复试错）
        /// / Verifies and one-shot consumes a captcha (consumed whether right or wrong, so stale tokens cannot be retried)
        /// </summary>
        /// <param name="token">生成时下发的令牌 / The token issued at generation</param>
        /// <param name="code">input 模式下用户输入的答案 / The answer typed by the user in input mode</param>
        /// <param name="points">click 模式下用户点击的自然像素坐标序列 / The user's click sequence in natural image pixels in click mode</param>
        /// <returns>成功 R；失败为 IncorrectVerificationCode(5201) 或 VerificationCodeExpired(5202) / Success R; failures carry IncorrectVerificationCode (5201) or VerificationCodeExpired (5202)</returns>
        R Verify(string? token, string? code, List<CaptchaPoint>? points);
    }
}
