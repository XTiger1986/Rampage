using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rampage.Web.HealthChecks
{
    /// <summary>
    /// 健康检查扩展：AddRampageHealthChecks 注册检查项（可继续 .AddCheck 链式追加），
    /// MapRampageHealthChecks 暴露 /health 端点（返回简明 JSON，含各检查项状态与耗时）/
    /// Health check extensions: AddRampageHealthChecks registers checks (chain further via .AddCheck);
    /// MapRampageHealthChecks exposes the /health endpoint (compact JSON with per-check status and duration).
    /// </summary>
    public static class RampageHealthCheckExtensions
    {
        /// <summary>
        /// 注册健康检查服务 / Registers the health check services.
        /// </summary>
        /// <param name="services">服务集合 / The service collection.</param>
        /// <returns>HealthChecks 构建器，可继续链式 AddCheck / The builder for chaining further checks.</returns>
        public static IHealthChecksBuilder AddRampageHealthChecks(this IServiceCollection services)
        {
            return services.AddHealthChecks();
        }

        /// <summary>
        /// 映射健康检查端点 / Maps the health check endpoint.
        /// </summary>
        /// <param name="endpoints">端点路由构建器 / The endpoint route builder.</param>
        /// <param name="path">端点路径，默认 /health / The endpoint path; default /health.</param>
        /// <returns>端点路由构建器 / The endpoint route builder.</returns>
        /// <remarks>执行全部已注册检查项，并以自定义 JSON 格式输出（status、totalDurationMs 及各检查项 entries 的 status/durationMs/description），替代框架默认的分布格式输出。</remarks>
        public static IEndpointRouteBuilder MapRampageHealthChecks(this IEndpointRouteBuilder endpoints, string path = "/health")
        {
            endpoints.MapHealthChecks(path, new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = WritePlainJson
            });
            return endpoints;
        }

        private static Task WritePlainJson(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            string entries = report.Entries.Count == 0
                ? string.Empty
                : ",\"entries\":{" + string.Join(",", report.Entries.Select(kv =>
                    $"\"{kv.Key}\":{{\"status\":\"{kv.Value.Status}\",\"durationMs\":{kv.Value.Duration.TotalMilliseconds:0},\"description\":\"{kv.Value.Description?.Replace("\"", "\\\"") ?? string.Empty}\"}}")) + "}";
            string json = $"{{\"status\":\"{report.Status}\",\"totalDurationMs\":{report.TotalDuration.TotalMilliseconds:0}{entries}}}";
            return context.Response.WriteAsync(json);
        }
    }
}
