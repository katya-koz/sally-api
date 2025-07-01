using Quartz.Impl;
using Quartz;
using SALLY_API.Walker.Jobs;

namespace SALLY_API.Walker
{
    public class ScheduleHelper
    {
        private Scheduler _scheduler;

        public ScheduleHelper(Scheduler scheduler)
        {
            _scheduler = scheduler;
        }
        public async Task RestartScheduler()
        {
            // Stop the current scheduler (if running)
            await StopScheduler();

            // Restart the scheduler
        }
        private async Task StopScheduler()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler();

            if (scheduler.IsStarted)
            {
                await scheduler.Shutdown();
            }
        }
        public static async Task ScheduleRestartJob()
        {
            // Create a scheduler factory
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler();

            // Define the job and tie it to the RestartScheduler class
            IJobDetail job = JobBuilder.Create<RestartWalker>()
                                        .WithIdentity("RestartSchedulerJob", "MaintenanceGroup")
                                        .Build();

            // Trigger the job to run at a specific time (e.g., daily at midnight)
            ITrigger trigger = TriggerBuilder.Create()
                                             .WithIdentity("RestartSchedulerTrigger", "MaintenanceGroup")
                                             .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Sunday,0, 0)) // Every day at 12:00 AM
                                             .Build();

            // Schedule the job
            await scheduler.ScheduleJob(job, trigger);

            // Start the scheduler
            await scheduler.Start();
        }


    }
}
