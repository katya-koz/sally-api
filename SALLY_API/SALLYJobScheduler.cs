using System;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Quartz.Impl;
using SALLY_API.Reports;
namespace SALLY_API
{

   // this is the quartz library scheduler
   // for now, it uses the date in email objects (recurring schedule object) to send them at that time: repeating every week
    public class SALLYJobScheduler
    {
        IScheduler scheduler; 
        
        public async Task StartSALLYJobScheduler()
        {
            GlobalLogger.Logger.Debug("starting scheduler...");
            using var emailTemplateWatcher = new FileSystemWatcher(ReportService.emailTemplatePath);

            emailTemplateWatcher.NotifyFilter = NotifyFilters.LastWrite;
            emailTemplateWatcher.Filter = "emailtemplates.json";
            emailTemplateWatcher.EnableRaisingEvents = true;

            emailTemplateWatcher.Changed += UpdateSchedule;


            StdSchedulerFactory factory = new StdSchedulerFactory();
            scheduler = await factory.GetScheduler();

            // and start it off
            await scheduler.Start();

            SetScheduledEmails(); // loop through the emailtemplates json, scan for emails that have a schedule object attached, and schedule them

            
        }
        public async Task ShutdownAsync() {
            GlobalLogger.Logger.Debug("shutting down scheduler");
            await scheduler.Shutdown();
        }
        private async void UpdateSchedule(object sender, FileSystemEventArgs e)
        {
            GlobalLogger.Logger.Debug("setting schedule up again...");
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            //else, we set schedule up again
            await this.ShutdownAsync();
            await this.StartSALLYJobScheduler();
        }

        private List<string> ParseRecurringScheduleToCronTrigger(KeyValuePair<DayOfWeek, List<TimeSpan>> dailySchedule)
        {
               
               

                List<string> cronExpressions = new List<string>();
            

                foreach (TimeSpan time in dailySchedule.Value) //iterate through all times in a day
                {
                    
                    // transform time and day into a cron expression, to repeat every week
                    cronExpressions.Add(time.Seconds.ToString() + " " + time.Minutes.ToString() + " " + time.Hours.ToString() + " ? * " + (int)(dailySchedule.Key + 1));

                }

          
            // return all of the cron expressions, there is now a cron expression for every time of that day in the schedule
            return cronExpressions;
            
        }
        private async void SetScheduledEmails()
        {
            using (ReportService reportService = new ReportService())
            {
                List<string> dailyCronTriggers;
                foreach (KeyValuePair<string, EmailTemplate> email in reportService.EmailTemplates) // need to iterate through all emailtemplates in json file
                {
                    EmailTemplate emailTemplate = email.Value;
                    // check if schedule is empty
                    if (emailTemplate.RecurringSchedule != null && emailTemplate.RecurringSchedule.Any())
                    {
                        foreach (var schedule in emailTemplate.RecurringSchedule) // now for every email template, we need to iterate through each day in the schedule...
                        {
                                    dailyCronTriggers = ParseRecurringScheduleToCronTrigger(schedule);

                            // another looop??? this time to loop through each hour of the day??? omg... this is crazy

                            foreach (string trigger in dailyCronTriggers)
                            {
                                string timeStr = trigger.Split(" ")[2] + trigger.Split(" ")[1]; // gets u time in this format : 1400
                                List<Report> attachments = reportService.GenerateAssociatedReports(email.Key);

                                JobDataMap dataMap = new JobDataMap();
                                dataMap["reports"] = attachments;
                                dataMap["recipients"] = emailTemplate.Recipients;
                                dataMap["ccRecipients"] = emailTemplate.CCRecipients;
                                dataMap["bccRecipients"] = emailTemplate.BCCRecipients;

                                IJobDetail emailJob = JobBuilder.Create<AutomaticEmailJob>()
                                    .WithIdentity("AutomaticEmailJob_" + email.Key + "_" + schedule.Key + "_" + timeStr)
                                    .UsingJobData("subject", emailTemplate.Subject)
                                    .UsingJobData("body", emailTemplate.Body)
                                    .UsingJobData(dataMap)
                                    .Build();

                                //GlobalLogger.Logger.Debug(trigger);
                                ITrigger emailTrigger = TriggerBuilder.Create()
                                    .WithIdentity("EmailTrigger_" + email.Key + "_" + schedule.Key + "_" + timeStr, "EmailGroup")
                                    .WithCronSchedule(trigger)
                                    .Build();

                                // Schedule the job
                                await scheduler.ScheduleJob(emailJob, emailTrigger);
                            }
                        }



                    }
        
                }

            } 

        }
        public class AutomaticEmailJob : IJob
        {


            public Task Execute(IJobExecutionContext context)
            {
                //parse context
                var reports = (List<Report>)context.JobDetail.JobDataMap["reports"];
                var recipients = (List<string>)context.JobDetail.JobDataMap["recipients"];
                var ccRecipients = (List<string>)context.JobDetail.JobDataMap["ccRecipients"];
                var bccRecipients = (List<string>)context.JobDetail.JobDataMap["bccRecipients"];
                var subject = context.JobDetail.JobDataMap.GetString("subject");
                var body = context.JobDetail.JobDataMap.GetString("body");

                var emailTemplate = new EmailTemplate
                {
                    Subject = subject,
                    Body = body,
                    Recipients = recipients,
                    CCRecipients = ccRecipients,
                    BCCRecipients = bccRecipients
                };

                //create email object
                Email automatedEmail = new Email(reports, emailTemplate);
                // then send it 
                automatedEmail.SendEmail();
                return Task.CompletedTask;
            }

        }
        
    }
}
