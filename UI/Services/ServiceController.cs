using System.Threading.Tasks;
using System.Threading;
using UI.Jobs;

namespace UI.Services
{
    public class ServiceController
    {
        private readonly PushSmsJob _pushSmsJob;

        public ServiceController(PushSmsJob pushSmsJob)
        {
            _pushSmsJob = pushSmsJob;
        }

        public async Task StartPushSmsJobAsync()
        {
            await _pushSmsJob.StartAsync(CancellationToken.None);
        }

        public async Task StopPushSmsJobAsync()
        {
            await _pushSmsJob.StopAsync(CancellationToken.None);
        }
    }
}