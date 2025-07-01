using Quartz;
using SALLY_API.Reports;

namespace SALLY_API.Walker.Jobs
{
    public class IPACJob: IJob

    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                using (ReportService report = new ReportService())
                {
                    await report.SendIPACReports();
                    GlobalLogger.Logger.Debug("IPAC Report sent at: " + DateTime.Now);

                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Debug("IPAC Job failed");
            }

        }
    }
}
