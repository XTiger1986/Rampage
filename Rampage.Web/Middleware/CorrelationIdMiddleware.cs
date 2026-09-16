using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace Rampage.Web.Middleware
{
    /**
// 链路
app.UseRampageCorrelationId();
endpoints.MapRampageHealthChecks();
     */

    /// <summary>
    /// 请求链路ID中间件：优先沿用请求头 X-Correlation-Id，否则生成短ID；
    /// 写入 HttpContext.Items、响应头与 AsyncLocal（供任意层读取，日志自动附带）。
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

        private static readonly AsyncLocal<string?> Current = new AsyncLocal<string?>();

        /// <summary>
        /// 当前请求的链路ID（中间件管道外为 null）
        /// </summary>
        public static string? CurrentId => Current.Value;

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string correlationId = context.Request.Headers[HeaderName].ToString();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N")[..16];
            }
            else if (correlationId.Length > 64)
            {
                correlationId = correlationId[..64];
            }

            context.Items[HeaderName] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            Current.Value = correlationId;
            try
            {
                await _next(context);
            }
            finally
            {
                Current.Value = null;
            }
        }
    }

    /// <summary>
    /// 链路ID中间件注册扩展
    /// </summary>
    public static class CorrelationIdApplicationBuilderExtensions
    {
        /// <summary>
        /// 启用请求链路ID（建议注册在管道最前部）
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRampageCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
