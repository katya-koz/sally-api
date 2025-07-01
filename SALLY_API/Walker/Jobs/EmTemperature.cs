using Quartz;

namespace SALLY_API.Walker.Jobs
{
    public class EMTemperature:IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                using (APIService api = new APIService())
                {
                    await api.EmailEMTemperatureReport();
                    GlobalLogger.Logger.Debug("EM data emailed at: " + DateTime.Now);
                    await Task.CompletedTask;
                }
            }
            catch (Exception ex) 
            {
                GlobalLogger.Logger.Debug("EM report job failed");
            }


        }

    }
}
