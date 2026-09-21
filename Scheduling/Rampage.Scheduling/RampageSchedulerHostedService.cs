using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Scheduling
{
    /// <summary>
    /// 宿主服务：随应用启停调度器（Web 场景注册 AddRampageScheduler 后自动生效） / Hosted service: starts and stops the scheduler with the host (takes effect automatically once AddRampageScheduler is registered)
    /// </summary>
    public sealed class RampageSchedulerHostedService : IHostedService
    {
        private readonly IRampageScheduler _scheduler;

        public RampageSchedulerHostedService(IRampageScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _scheduler.StartAsync(cancellationToken);
        }

        public Task StopAsync(System.Threading.CancellationToken cancellationToken)
        {
            return _scheduler.StopAsync();
        }
    }
}
