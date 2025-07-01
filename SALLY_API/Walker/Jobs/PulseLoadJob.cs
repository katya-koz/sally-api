using Quartz;
namespace SALLY_API.Walker.Jobs
{
    public class PulseLoadJob:IJob
    {
        public async Task Execute(IJobExecutionContext context )
        {
            try
            {
                using (APIService api = new APIService())
                {
                    await api.LoadPulseTagReport();
    //                await api.LoadPulseHHReport(); DO NOT UNCOMMENT THIS ITS BROKEN AND BREAKS THE LoadPulseTagReport job 
                    //GlobalLogger.Logger.Debug("Pulse data loaded at: " + DateTime.Now);
                    await Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Debug("Pulse Jobs failed");
            }


        }

    }
}
