using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Resilience
{
    /// <summary>
    /// 熔断打开期间抛出的异常
    /// </summary>
    public sealed class BrokenCircuitException : Exception
    {
        /// <summary>距熔断自动结束（进入半开）的剩余时间</summary>
        public TimeSpan RetryAfter { get; }

        public BrokenCircuitException(string operationKey, TimeSpan retryAfter)
            : base($"操作 '{operationKey}' 的熔断器处于打开状态，{retryAfter.TotalSeconds:0.#} 秒后进入半开探测")
        {
            RetryAfter = retryAfter;
        }
    }

    /// <summary>
    /// 弹性执行 Provider 接口：默认实现为内置的重试+熔断+超时（DefaultResilienceProvider）；
    /// 后续如需接入 Polly 等第三方，实现本接口替换注册即可，业务代码零改动。
    /// </summary>
    public interface IResilienceProvider
    {
        /// <summary>
        /// 按策略执行操作：重试、熔断（按 operationKey 隔离）、超时。
        /// 调用方 CancellationToken 取消时立即抛出 OperationCanceledException，不重试。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operationKey">操作键（熔断器按此隔离，同名操作共享熔断状态）</param>
        /// <param name="policy">策略配置</param>
        /// <param name="operation">业务委托</param>
        /// <param name="cancellationToken"></param>
        Task<T> ExecuteAsync<T>(string operationKey, ResiliencePolicy policy, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按策略执行无返回值操作
        /// </summary>
        Task ExecuteAsync(string operationKey, ResiliencePolicy policy, Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    }
}
