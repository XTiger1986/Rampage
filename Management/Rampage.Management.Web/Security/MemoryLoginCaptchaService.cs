using Rampage.Drawing.VerificationCode;
using Rampage.Model;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.Management.Security
{
    /// <summary>
    /// 进程内登录验证码服务（令牌 → 答案暂存 + 一次性消费）。单实例部署可直接使用；
    /// 多实例下每实例独立存答案，需负载均衡粘性会话或在扩展库注册分布式实现。
    /// / In-process login captcha service (token → answer store + one-shot consumption). Usable as-is for single-instance deployments;
    /// in multi-instance setups each instance stores answers independently, so use sticky sessions or register a distributed implementation.
    /// </summary>
    /// <remarks>
    /// 暂存结构为 ConcurrentDictionary（键为一次性令牌），核验经 TryRemove 原子取出即消费；
    /// 过期条目在每次生成时顺带清理（每分钟至多一次）。
    /// The store is a ConcurrentDictionary keyed by one-time tokens; verification consumes atomically via TryRemove;
    /// expired entries are cleaned opportunistically on each generation (at most once a minute).
    /// </remarks>
    public class MemoryLoginCaptchaService : ILoginCaptchaService
    {
        private const string InputMode = "input";
        private const string ClickMode = "click";
        private const string ClickTip = "请按提示依次点击";

        /// <summary>
        /// 一条验证码暂存记录：交互模式与待比对载荷（图形型为答案字符串，点选型为 ClickCodeResult 的 JSON）
        /// / One stored captcha entry: the interaction mode and the payload to compare against (the answer string for graphic styles, ClickCodeResult JSON for click styles)
        /// </summary>
        private sealed class CaptchaEntry
        {
            public bool IsClick;
            public string Payload = string.Empty;
            public DateTime ExpireAtUtc;
        }

        private readonly ConcurrentDictionary<string, CaptchaEntry> _captchas = new ConcurrentDictionary<string, CaptchaEntry>(StringComparer.Ordinal);
        private readonly Func<LoginCaptchaOptions> _optionsProvider;
        private DateTime _lastCleanupUtc = DateTime.UtcNow;

        /// <summary>
        /// 全局默认实例（未在 DI 注册 ILoginCaptchaService 时由控制器直接使用）
        /// / Global default instance (used directly by controllers when ILoginCaptchaService is not registered in DI)
        /// </summary>
        public static MemoryLoginCaptchaService Default { get; } = new MemoryLoginCaptchaService();

        /// <summary>
        /// 使用全局配置构造（每次操作经 LoginCaptchaOptions.Load 读取最新配置）
        /// / Construct with global configuration (options are re-read via LoginCaptchaOptions.Load on each call)
        /// </summary>
        public MemoryLoginCaptchaService() : this(LoginCaptchaOptions.Load)
        {
        }

        /// <summary>
        /// 使用自定义选项提供器构造（便于测试注入）/ Construct with a custom options provider (handy for tests)
        /// </summary>
        /// <param name="optionsProvider">每次操作时返回最新配置的委托 / Delegate returning the latest options on each call</param>
        /// <exception cref="ArgumentNullException">optionsProvider 为 null / optionsProvider is null</exception>
        public MemoryLoginCaptchaService(Func<LoginCaptchaOptions> optionsProvider)
        {
            _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        }

        /// <summary>
        /// 生成一张登录验证码：按配置类型集合随机选型，图形型（1–10）经 Manager 新重载取得答案与提示，点选型（11/12）以 ClickCodeResult JSON 为载荷
        /// / Generates a login captcha: draw a random type from the configured set; graphic styles (1–10) get the answer and tip via the manager's tip overload, click styles (11/12) store ClickCodeResult JSON as the payload
        /// </summary>
        /// <returns>验证码图片响应 / The captcha image payload</returns>
        /// <exception cref="InvalidOperationException">底层生成失败（图片或答案为空）/ The underlying generation failed (empty image or answer)</exception>
        public LoginCaptchaImage Generate()
        {
            LoginCaptchaOptions options = _optionsProvider();
            int[] types = options.ResolveEffectiveTypes();
            // 与 VerificationCodeManager 的选型随机源保持一致（每次调用新建种子）
            // Keep the same random source style as VerificationCodeManager (a fresh seeded instance per call)
            int codeType = types[new Random(Guid.NewGuid().GetHashCode()).Next(0, types.Length)];

            byte[]? imageBytes;
            bool isClick;
            string payload;
            string mode;
            string tip;
            if (codeType == LoginCaptchaOptions.ClickTextType || codeType == LoginCaptchaOptions.ClickWordType)
            {
                ClickCodeResult clickResult;
                if (codeType == LoginCaptchaOptions.ClickTextType)
                {
                    imageBytes = VerificationCodeManager.CreateClickVerificationCodeImage(out clickResult);
                }
                else
                {
                    imageBytes = VerificationCodeManager.CreateWordClickVerificationCodeImage(out clickResult);
                }
                payload = clickResult.ToJson();
                isClick = true;
                mode = ClickMode;
                tip = ClickTip;
            }
            else
            {
                imageBytes = VerificationCodeManager.CreateVerificationCodeImage(new int[] { codeType }, out string? resultCode, out string? resultTip);
                payload = resultCode ?? string.Empty;
                isClick = false;
                mode = InputMode;
                tip = resultTip ?? string.Empty;
            }

            if (imageBytes == null || imageBytes.Length == 0 || string.IsNullOrEmpty(payload))
            {
                throw new InvalidOperationException($"Login captcha generation failed for type {codeType}.");
            }

            DateTime now = DateTime.UtcNow;
            string token = Guid.NewGuid().ToString("N");
            int expireSeconds = options.ExpireSeconds > 0 ? options.ExpireSeconds : 300;
            _captchas[token] = new CaptchaEntry
            {
                IsClick = isClick,
                Payload = payload,
                ExpireAtUtc = now.AddSeconds(expireSeconds)
            };
            CleanupExpired(now);

            return new LoginCaptchaImage
            {
                Token = token,
                Base64 = Convert.ToBase64String(imageBytes),
                Mode = mode,
                Tip = tip
            };
        }

        /// <summary>
        /// 核验并一次性消费验证码：令牌缺失或不存在按不正确处理（不区分过期，防探测）；
        /// 图形型忽略大小写比对，点选型经 ClickCodeResult.Verify 按序命中判定
        /// / Verifies and one-shot consumes a captcha: a missing or unknown token counts as incorrect (no expiry distinction, to resist probing);
        /// graphic styles compare case-insensitively, click styles judge via ClickCodeResult.Verify's ordered hit test
        /// </summary>
        /// <param name="token">生成时下发的令牌 / The token issued at generation</param>
        /// <param name="code">input 模式下用户输入的答案 / The answer typed by the user in input mode</param>
        /// <param name="points">click 模式下用户点击的自然像素坐标序列 / The user's click sequence in natural image pixels in click mode</param>
        /// <returns>成功 R；失败为 IncorrectVerificationCode(5201) 或 VerificationCodeExpired(5202) / Success R; failures carry IncorrectVerificationCode (5201) or VerificationCodeExpired (5202)</returns>
        public R Verify(string? token, string? code, List<CaptchaPoint>? points)
        {
            if (string.IsNullOrWhiteSpace(token) || !_captchas.TryRemove(token.Trim(), out CaptchaEntry? entry))
            {
                return R.Create(ResultCode.IncorrectVerificationCode);
            }
            if (DateTime.UtcNow > entry.ExpireAtUtc)
            {
                return R.Create(ResultCode.VerificationCodeExpired);
            }
            if (!entry.IsClick)
            {
                return string.Equals(entry.Payload.Trim(), code?.Trim(), StringComparison.OrdinalIgnoreCase)
                    ? R.Success()
                    : R.Create(ResultCode.IncorrectVerificationCode);
            }
            ClickCodeResult? clickResult = ClickCodeResult.FromJson(entry.Payload);
            if (clickResult == null || points == null || points.Count == 0)
            {
                return R.Create(ResultCode.IncorrectVerificationCode);
            }
            SKPoint[] clickedPoints = points.Select(p => new SKPoint(p.X, p.Y)).ToArray();
            return clickResult.Verify(clickedPoints)
                ? R.Success()
                : R.Create(ResultCode.IncorrectVerificationCode);
        }

        /// <summary>
        /// 顺带清理过期条目（每分钟至多一次，避免高频遍历）
        /// / Opportunistically remove expired entries (at most once a minute, avoiding frequent sweeps)
        /// </summary>
        /// <param name="now">当前 UTC 时间 / The current UTC time</param>
        private void CleanupExpired(DateTime now)
        {
            if (now - _lastCleanupUtc < TimeSpan.FromMinutes(1))
            {
                return;
            }
            _lastCleanupUtc = now;
            foreach (KeyValuePair<string, CaptchaEntry> kv in _captchas)
            {
                if (kv.Value.ExpireAtUtc <= now)
                {
                    _captchas.TryRemove(kv.Key, out _);
                }
            }
        }
    }
}
