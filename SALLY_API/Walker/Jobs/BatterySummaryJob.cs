using Quartz;
using SALLY_API.Reports;

namespace SALLY_API.Walker.Jobs
{
    public class BatterySummaryJob:IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                using (ReportService report = new ReportService())
                {
                    await report.SendBatterySummaryReports();
                    GlobalLogger.Logger.Debug("Battery Summary Report sent at: " + DateTime.Now);

                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Debug("Battery Summary Job failed");
            }


        }

    }

}
