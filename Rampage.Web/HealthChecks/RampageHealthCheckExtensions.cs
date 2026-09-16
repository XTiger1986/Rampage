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
    /// MapRampageHealthChecks 暴露 /health 端点（返回简明 JSON，含各检查项状态与耗时）。
    /// </summary>
    public static class RampageHealthCheckExtensions
    {
        /// <summary>
        /// 注册健康检查服务
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddRampageHealthChecks(this IServiceCollection services)
        {
            return services.AddHealthChecks();
        }

        /// <summary>
        /// 映射健康检查端点
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="path">默认 /health</param>
        /// <returns></returns>
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
