using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Resilience
{
    /// <summary>
    /// 内置弹性执行实现：指数退避重试 + 连续失败熔断（半开探测）+ 单次执行超时。 / Built-in resilience implementation: exponential-backoff retry + circuit breaker on consecutive failures (with half-open probing) + per-execution timeout.
    /// </summary>
    public sealed class DefaultResilienceProvider : IResilienceProvider
    {
        private readonly ConcurrentDictionary<string, CircuitState> _circuits = new ConcurrentDictionary<string, CircuitState>(StringComparer.OrdinalIgnoreCase);

        public Task ExecuteAsync(string operationKey, ResiliencePolicy policy, Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<object?>(operationKey, policy, async ct =>
            {
                await operation(ct).ConfigureAwait(false);
                return null;
            }, cancellationToken);
        }

        public async Task<T> ExecuteAsync<T>(string operationKey, ResiliencePolicy policy, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(operationKey))
            {
                throw new ArgumentException("operationKey is required.", nameof(operationKey));
            }
            operation ??= _ => Task.FromResult<T>(default!);
            policy ??= ResiliencePolicy.Default;
            CircuitState circuit = policy.BreakerFailureThreshold > 0
                ? _circuits.GetOrAdd(operationKey, _ => new CircuitState(policy.BreakerFailureThreshold, policy.BreakerBreakDuration))
                : CircuitState.NoOp;

            int attempt = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                circuit.BeforeCall(operationKey);

                CancellationTokenSource? timeoutCts = null;
                try
                {
                    CancellationToken effectiveToken = cancellationToken;
                    if (policy.Timeout is TimeSpan timeout)
                    {
                        timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(timeout);
                        effectiveToken = timeoutCts.Token;
                    }
                    T result = await operation(effectiveToken).ConfigureAwait(false);
                    circuit.RecordSuccess();
                    return result;
                }
                catch (BrokenCircuitException)
                {
                    throw;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // 调用方主动取消：立即上抛，不计失败不重试 / Caller-initiated cancellation: rethrow immediately, neither counting a failure nor retrying
                    throw;
                }
                catch (Exception)
                {
                    circuit.RecordFailure();
                    if (attempt >= policy.MaxRetries)
                    {
                        throw;
                    }
                    attempt++;
                    TimeSpan delay = policy.UseExponentialBackoff
                        ? TimeSpan.FromMilliseconds(policy.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1))
                        : policy.RetryBaseDelay;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    timeoutCts?.Dispose();
                }
            }
        }

        /// <summary>
        /// 熔断器状态机：Closed（正常）→ 连续失败达阈值 → Open（拒绝请求）→ 超时后 HalfOpen（放行一次探测）→ 成功回 Closed / 失败回 Open / Circuit-breaker state machine: Closed (normal) -&gt; Open (reject requests) after the consecutive-failure threshold -&gt; HalfOpen after the break duration (one probe admitted) -&gt; Closed on success / Open on failure
        /// </summary>
        private sealed class CircuitState
        {
            public static readonly CircuitState NoOp = new CircuitState(0, TimeSpan.Zero);

            private readonly int _failureThreshold;
            private readonly TimeSpan _breakDuration;
            private readonly object _lock = new object();
            private int _consecutiveFailures;
            private DateTime _openedUntilUtc = DateTime.MinValue;
            private bool _halfOpenProbeInFlight;

            public CircuitState(int failureThreshold, TimeSpan breakDuration)
            {
                _failureThreshold = failureThreshold;
                _breakDuration = breakDuration;
            }

            public void BeforeCall(string operationKey)
            {
                if (_failureThreshold <= 0)
                {
                    return;
                }
                lock (_lock)
                {
                    DateTime nowUtc = DateTime.UtcNow;
                    if (nowUtc < _openedUntilUtc)
                    {
                        throw new BrokenCircuitException(operationKey, _openedUntilUtc - nowUtc);
                    }
                    if (_consecutiveFailures >= _failureThreshold)
                    {
                        // 半开：仅放行一个探测请求，其余继续拒绝 / Half-open: admit exactly one probe request and keep rejecting the rest
                        if (_halfOpenProbeInFlight)
                        {
                            throw new BrokenCircuitException(operationKey, TimeSpan.Zero);
                        }
                        _halfOpenProbeInFlight = true;
                    }
                }
            }

            public void RecordSuccess()
            {
                if (_failureThreshold <= 0)
                {
                    return;
                }
                lock (_lock)
                {
                    _consecutiveFailures = 0;
                    _openedUntilUtc = DateTime.MinValue;
                    _halfOpenProbeInFlight = false;
                }
            }

            public void RecordFailure()
            {
                if (_failureThreshold <= 0)
                {
                    return;
                }
                lock (_lock)
                {
                    _consecutiveFailures++;
                    _halfOpenProbeInFlight = false;
                    if (_consecutiveFailures >= _failureThreshold)
                    {
                        _openedUntilUtc = DateTime.UtcNow + _breakDuration;
                    }
                }
            }
        }
    }
}
