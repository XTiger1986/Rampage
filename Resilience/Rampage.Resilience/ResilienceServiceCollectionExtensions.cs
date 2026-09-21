using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rampage.Resilience
{
    /**
// 弹性 / Resilience
var result = await resilience.ExecuteAsync("query-user", new ResiliencePolicy { MaxRetries = 3, Timeout = TimeSpan.FromSeconds(3) }, async ct => await QueryAsync(ct));
     * */

    /// <summary>
    /// 弹性组件注册扩展 / Registration extensions for the resilience component
    /// </summary>
    public static class ResilienceServiceCollectionExtensions
    {
        /// <summary>
        /// 注册内置弹性 Provider（重试+熔断+超时）。 / Registers the built-in resilience provider (retry + circuit breaker + timeout).
        /// 如需接入第三方实现（如 Polly 适配器），实现 IResilienceProvider 后用 / To adopt a third-party implementation (e.g. a Polly adapter), implement IResilienceProvider and then
        /// services.AddSingleton&lt;IResilienceProvider, XxxProvider&gt;() 覆盖注册即可。 / override the registration with services.AddSingleton&lt;IResilienceProvider, XxxProvider&gt;().
        /// </summary>
        /// <param name="services">服务集合 / The service collection</param>
        /// <returns>服务集合（便于链式调用） / The service collection, for chaining</returns>
        public static IServiceCollection AddRampageResilience(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            return services.AddSingleton<IResilienceProvider, DefaultResilienceProvider>();
        }
    }
}
