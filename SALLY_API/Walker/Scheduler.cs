using Quartz;
using Quartz.Impl;
using SALLY_API.Walker.Jobs;


namespace SALLY_API.Walker
{
    public class Scheduler : IHostedService
    {
        private IScheduler _scheduler;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
                _scheduler = await schedulerFactory.GetScheduler(cancellationToken);

                await ConfigureJobs();

                await _scheduler.Start(cancellationToken);
                GlobalLogger.Logger.Debug("Scheduler started successfully.");
            }
            catch (SchedulerException ex)
            {
                GlobalLogger.Logger.Debug($"Scheduler Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Debug($"General Exception: {ex.Message}");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_scheduler != null)
            {
                await _scheduler.Shutdown(cancellationToken);
                GlobalLogger.Logger.Debug("Scheduler stopped successfully.");
            }
        }

        private async Task ConfigureJobs()
        {
            // Schedule PulseLoadJob: Every hour on the hour
            await ScheduleJob<PulseLoadJob>(
                jobName: "PulseLoadJob",
                jobGroup: "JobGroup",
                trigger: CreateCronTrigger("PulseLoadTrigger", "TriggerGroup", "0 0 * * * ?"));

            // Schedule IPACReportJob: Every Monday at 10:00 AM
            await ScheduleJob<IPACJob>(
                jobName: "IPACReportJob",
                jobGroup: "ReportGroup",
                trigger: 
            CreateWeeklyTrigger("TestTrigger", "TriggerGroup",DayOfWeek.Monday,10,45));


            await ScheduleJob<BatteryLoadJob>(
                jobName: "DeadBatteryLoadJob",
                jobGroup: "JobGroup",
                trigger: CreateDailyTrigger("DailyExecution", "DailyTriggers", 10, 45));
           // CreateWeeklyTrigger("WeeklyMonday", "WeeklyReports", DayOfWeek.Monday, 9, 15 ));

            // Schedule BatterySummaryJob: Run immediately
            //await ScheduleJob<BatterySummaryJob>(
            //    jobName: "BatteryReport",
            //    jobGroup: "ReportGroup",
            //    trigger: CreateImmediateTrigger("TestTrigger", "TriggerGroup"));

            // Schedule EMTemperature job: Run daily at 7:00 AM and 8:00 PM
            /*            //await ScheduleJob<EMTemperature>(
                        //    jobName: "EMTemperatureJob",
                        //    jobGroup: "JobGroup",
                        //    triggers: new List<ITrigger>
                        //    {
                        //CreateDailyTrigger("Daily7AM", "TriggerGroup", 7, 0),
                        //CreateDailyTrigger("Daily8PM", "TriggerGroup", 20, 0)
                        //    },
                        //    replace: true);*/

            // Define HandHygieneCleanUp job (not scheduled)
            IJobDetail HHCleanUp = CreateJob<HandHygieneCleanUp>("CentrakJob", "JobGroup");
            // Optionally schedule HHCleanUp later
        }

        // Helper to create a job detail
        private IJobDetail CreateJob<T>(string jobName, string jobGroup) where T : IJob
        {
            return JobBuilder.Create<T>()
                             .WithIdentity(jobName, jobGroup)
                             .Build();
        }


        // Helper to schedule a job with a single trigger
        private async Task ScheduleJob<T>(string jobName, string jobGroup, ITrigger trigger) where T : IJob
        {
            IJobDetail job = CreateJob<T>(jobName, jobGroup);
            await _scheduler.ScheduleJob(job, trigger);
        }

        // Helper to schedule a job with multiple triggers
        private async Task ScheduleJob<T>(string jobName, string jobGroup, List<ITrigger> triggers, bool replace) where T : IJob
        {
            IJobDetail job = CreateJob<T>(jobName, jobGroup);
            await _scheduler.ScheduleJob(job, triggers, replace);
        }

        // Helper to create a cron trigger
        private ITrigger CreateCronTrigger(string triggerName, string triggerGroup, string cronExpression)
        {
            return TriggerBuilder.Create()
                                 .WithIdentity(triggerName, triggerGroup)
                                 .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression))
                                 .Build();
        }



        // Helper to create a daily trigger at a specified time
        private ITrigger CreateDailyTrigger(string triggerName, string triggerGroup, int hour, int minute)
        {
            return TriggerBuilder.Create()
                                 .WithIdentity(triggerName, triggerGroup)
                                 .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(hour, minute))
                                 .Build();
        }

        // Helper to create a weekly trigger at a specified day and time
        private ITrigger CreateWeeklyTrigger(string triggerName, string triggerGroup, DayOfWeek day, int hour, int minute)
        {
            return TriggerBuilder.Create()
                                 .WithIdentity(triggerName, triggerGroup)
                                 .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(day, hour, minute))
                                 .Build();
        }

        // Helper to create an immediate trigger
        private ITrigger CreateImmediateTrigger(string triggerName, string triggerGroup)
        {
            return TriggerBuilder.Create()
                                 .WithIdentity(triggerName, triggerGroup)
                                 .StartNow()
                                 .Build();
        }


    }
}
