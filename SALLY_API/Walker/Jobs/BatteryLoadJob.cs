using Quartz;

namespace SALLY_API.Walker.Jobs
{
    public class BatteryLoadJob:IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                using (APIService api = new APIService())
                {
                    GlobalLogger.Logger.Debug("starting Pulse data load at: " + DateTime.Now);

                    await api.LoadPulseTagReport();
                    await Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error("Pulse Jobs failed");
            }


        }

    }
}
