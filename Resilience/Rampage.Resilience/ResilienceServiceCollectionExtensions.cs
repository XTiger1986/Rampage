using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rampage.Resilience
{
    /**
// 弹性
var result = await resilience.ExecuteAsync("query-user", new ResiliencePolicy { MaxRetries = 3, Timeout = TimeSpan.FromSeconds(3) }, async ct => await QueryAsync(ct));
     * */

    /// <summary>
    /// 弹性组件注册扩展
    /// </summary>
    public static class ResilienceServiceCollectionExtensions
    {
        /// <summary>
        /// 注册内置弹性 Provider（重试+熔断+超时）。
        /// 如需接入第三方实现（如 Polly 适配器），实现 IResilienceProvider 后用
        /// services.AddSingleton&lt;IResilienceProvider, XxxProvider&gt;() 覆盖注册即可。
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
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
