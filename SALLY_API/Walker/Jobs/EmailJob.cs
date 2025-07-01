using Quartz;

namespace SALLY_API.Walker.Jobs
{
    public class OutDatedBadgeFirmwareReportJob:IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            APIService api = new APIService();
            await api.EmailEMTemperatureReport();
            api.Dispose();
            GlobalLogger.Logger.Debug("EM data emailed at: " + DateTime.Now);
            await Task.CompletedTask;


        }


    }
}
