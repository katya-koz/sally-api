namespace SALLY_API
{
    public class SALLYHostedService : IHostedService
    {
        private readonly SALLYJobScheduler _jobScheduler;

        public SALLYHostedService(SALLYJobScheduler jobScheduler)
        {
            GlobalLogger.Logger.Debug("hosted constructer");
            _jobScheduler = jobScheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            GlobalLogger.Logger.Debug("starting");
            await _jobScheduler.StartSALLYJobScheduler();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _jobScheduler.ShutdownAsync();
        }
    }
}
