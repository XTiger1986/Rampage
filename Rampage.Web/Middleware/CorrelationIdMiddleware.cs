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
    /// Correlation id middleware: reuses the X-Correlation-Id request header when present, otherwise generates a short id;
    /// publishes it to HttpContext.Items, the response header and an AsyncLocal (readable anywhere; logging picks it up automatically).
    /// 注意：RampageWebApplication 不自动挂载本中间件，需要时显式调用 UseRampageCorrelationId()。
    /// Note: RampageWebApplication does not wire this up automatically; call UseRampageCorrelationId() explicitly when needed.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        /// <summary>
        /// 链路ID请求/响应头名称 / The correlation id request/response header name
        /// </summary>
        public const string HeaderName = "X-Correlation-Id";

        private static readonly AsyncLocal<string?> Current = new AsyncLocal<string?>();

        /// <summary>
        /// 当前请求的链路ID（中间件管道外为 null）/ The correlation id of the current request (null outside the middleware pipeline)
        /// </summary>
        public static string? CurrentId => Current.Value;

        private readonly RequestDelegate _next;

        /// <summary>
        /// 构造函数 / Constructor
        /// </summary>
        /// <param name="next">管道中的下一个委托 / The next delegate in the pipeline</param>
        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// 解析/生成链路ID：入站超长（>64）时截断，写入 Items、响应头与 AsyncLocal 后继续管道 / Resolves/generates the correlation id: truncates inbound values longer than 64 chars, stores it in Items, the response header and the AsyncLocal, then continues the pipeline
        /// </summary>
        /// <param name="context">当前 HTTP 上下文 / The current HTTP context</param>
        /// <returns>异步任务 / A task representing the operation</returns>
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
    /// 链路ID中间件注册扩展 / Registration extensions for the correlation id middleware
    /// </summary>
    public static class CorrelationIdApplicationBuilderExtensions
    {
        /// <summary>
        /// 启用请求链路ID（建议注册在管道最前部）/ Enables the request correlation id (best registered at the very front of the pipeline)
        /// </summary>
        /// <param name="app">应用构建器 / The application builder</param>
        /// <returns>应用构建器 / The application builder</returns>
        public static IApplicationBuilder UseRampageCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
