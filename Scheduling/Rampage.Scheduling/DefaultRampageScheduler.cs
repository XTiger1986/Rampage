using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Scheduling
{
    /// <summary>
    /// 内置调度器：按本地时间分钟对齐的后台循环触发 Cron 任务。
    /// 同一任务上一次执行未结束时跳过本次触发（不排队）；触发前通过 <see cref="IJobLockProvider"/> 获取任务锁，
    /// 注册分布式锁提供者后可在多实例部署下保证同一任务只在一个实例执行。
    /// </summary>
    public sealed class DefaultRampageScheduler : IRampageScheduler
    {
        private readonly IJobLockProvider _lockProvider;
        private readonly RampageSchedulerOptions _options;

        public DefaultRampageScheduler() : this(null, null)
        {
        }

        public DefaultRampageScheduler(IJobLockProvider? lockProvider, RampageSchedulerOptions? options)
        {
            _lockProvider = lockProvider ?? new LocalJobLockProvider();
            _options = options ?? new RampageSchedulerOptions();
        }

        private sealed class JobEntry
        {
            public string Name = string.Empty;
            public CronExpression Cron = null!;
            public Func<CancellationToken, Task> Work = _ => Task.CompletedTask;
            public bool RunImmediately;
            public int Running;
            public DateTime? LastRunTime;
            public string? LastError;
        }

        private readonly ConcurrentDictionary<string, JobEntry> _jobs = new ConcurrentDictionary<string, JobEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly object _startLock = new object();
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private bool _started;

        public void Register(string name, string cron, Func<CancellationToken, Task> job, bool runImmediately = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Job name is required.", nameof(name));
            }
            job ??= _ => Task.CompletedTask;
            CronExpression cronExpression = CronExpression.Parse(cron);
            _jobs[name] = new JobEntry { Name = name, Cron = cronExpression, Work = job, RunImmediately = runImmediately };
        }

        public void Register(string name, string cron, Action job, bool runImmediately = false)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }
            Register(name, cron, _ => { job(); return Task.CompletedTask; }, runImmediately);
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_startLock)
            {
                if (_started)
                {
                    return Task.CompletedTask;
                }
                _started = true;
                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;
                _loopTask = Task.Run(() => RunLoopAsync(token), CancellationToken.None);
            }
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            Task? loopTask;
            lock (_startLock)
            {
                if (!_started)
                {
                    return;
                }
                loopTask = _loopTask;
                _cts?.Cancel();
            }
            if (loopTask != null)
            {
                try
                {
                    await loopTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
                {
                }
            }
        }

        public List<ScheduledJobInfo> GetJobs()
        {
            return _jobs.Values.Select(job => new ScheduledJobInfo
            {
                Name = job.Name,
                Cron = job.Cron.ToString(),
                Running = Volatile.Read(ref job.Running) == 1,
                LastRunTime = job.LastRunTime,
                LastError = job.LastError,
                NextRunTime = SafeNextOccurrence(job)
            }).ToList();
        }

        private static DateTime? SafeNextOccurrence(JobEntry job)
        {
            try
            {
                return job.Cron.NextOccurrence(DateTime.Now);
            }
            catch
            {
                return null;
            }
        }

        private async Task RunLoopAsync(CancellationToken cancellationToken)
        {
            // 启动时先执行标记了立即执行的任务
            foreach (JobEntry job in _jobs.Values)
            {
                if (job.RunImmediately)
                {
                    TriggerJob(job, cancellationToken);
                }
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;
                DateTime nextMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
                try
                {
                    await Task.Delay(nextMinute - now, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                foreach (JobEntry job in _jobs.Values)
                {
                    if (job.Cron.Matches(nextMinute))
                    {
                        TriggerJob(job, cancellationToken);
                    }
                }
            }
        }

        private void TriggerJob(JobEntry job, CancellationToken cancellationToken)
        {
            // 上一次仍在执行：跳过本次触发
            if (Interlocked.CompareExchange(ref job.Running, 1, 0) != 0)
            {
                return;
            }
            _ = Task.Run(async () =>
            {
                IDisposable? jobLock = null;
                try
                {
                    TimeSpan expiry = _options.JobLockExpiry > TimeSpan.Zero ? _options.JobLockExpiry : TimeSpan.FromMinutes(10);
                    jobLock = await _lockProvider.TryAcquireAsync(job.Name, expiry, cancellationToken).ConfigureAwait(false);
                    if (jobLock == null)
                    {
                        // 其它实例正在执行该任务：本实例跳过本次触发
                        return;
                    }

                    await job.Work(cancellationToken).ConfigureAwait(false);
                    job.LastRunTime = DateTime.Now;
                    job.LastError = null;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    job.LastRunTime = DateTime.Now;
                    job.LastError = ex.ToString();
                }
                finally
                {
                    try
                    {
                        jobLock?.Dispose();
                    }
                    catch
                    {
                        // 锁释放失败由 TTL 兜底
                    }
                    Volatile.Write(ref job.Running, 0);
                }
            }, CancellationToken.None);
        }
    }
}
