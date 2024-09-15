using System.Threading.Tasks;
using System.Threading;
using UI.Jobs;

namespace UI.Services
{
    public class BackgroundServiceManager
    {
            private readonly PushSmsJob _pushSmsJob;

            public BackgroundServiceManager(PushSmsJob pushSmsJob)
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
