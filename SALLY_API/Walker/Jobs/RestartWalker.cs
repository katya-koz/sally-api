using Quartz;

namespace SALLY_API.Walker.Jobs
{
    public class RestartWalker : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Scheduler scheduler = new Scheduler();
            ScheduleHelper manager = new ScheduleHelper(scheduler);
            await manager.RestartScheduler();
        }

    }
}
