using DocumentFormat.OpenXml.Drawing;
using Quartz;
using System.Net.Mail;

namespace SALLY_API.Reports
{
    internal class Email : IDisposable
    {
        private string subject { get; set; }
        private string body { get; set; }
        private List<Attachment> attachments { get; set; }
        private List<string> recipients { get; set; } // email address strings
        private List<string> CCRecipients { get; set; } = new List<string>();
        private List<string> BCCRecipients { get; set; } = new List<string>();

        private Dictionary<DayOfWeek, List<TimeSpan>> RecurringSchedule = new Dictionary<DayOfWeek, List<TimeSpan>>();

        public Email(List<Report> reports, EmailTemplate emailTemplate)
        {
            GlobalLogger.Logger.Debug("email constructor!");

            this.body = emailTemplate.Body;
            this.subject = emailTemplate.Subject;
            this.recipients = emailTemplate.Recipients;
            this.CCRecipients = emailTemplate.CCRecipients;
            this.BCCRecipients = emailTemplate.BCCRecipients;
            this.RecurringSchedule = emailTemplate.RecurringSchedule;
            BuildAttachments(reports);

        }
        public Email(Report report, EmailTemplate emailTemplate)
        {
            GlobalLogger.Logger.Debug("email constructor!");
            this.body = emailTemplate.Body;
            this.subject = emailTemplate.Subject;
            this.recipients = emailTemplate.Recipients;
            this.CCRecipients = emailTemplate.CCRecipients;
            this.BCCRecipients = emailTemplate.BCCRecipients;
            this.RecurringSchedule = emailTemplate.RecurringSchedule;
            BuildAttachment(report);

        }
        private void BuildAttachments(List<Report> reports)
        {
            attachments = new List<Attachment>();
            foreach (Report report in reports)
            {
                //string reportFilePath = report.DownloadReport();
                Attachment attachment = new Attachment(report.report, report.name + ".xlsx");
                attachments.Add(attachment);
            }
        }

        private void BuildAttachment(Report report)
        {
            attachments = new List<Attachment>();
                //string reportFilePath = report.DownloadReport();
                Attachment attachment = new Attachment(report.report, report.name + ".xlsx");
                attachments.Add(attachment);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);

        }

        public void SendEmail()
        {

            using (SmtpClient smtpClient = new SmtpClient("MHSMTP.mackenziehealth.local"))
            {
                smtpClient.UseDefaultCredentials = true;

                MailAddress from = new MailAddress("application.integration@mackenziehealth.ca", "No Reply");
                
                MailMessage message = new MailMessage();

                foreach (Attachment a in attachments)
                {
                    message.Attachments.Add(a);
                }

                foreach (string recipient in recipients)
                {
                    message.To.Add(recipient);
                }

                foreach (string recipient in CCRecipients)
                {
                    message.CC.Add(recipient);
                }

                foreach (string recipient in BCCRecipients)
                {
                    message.Bcc.Add(recipient);
                }

                message.Body = body;
                message.Subject = subject;
                message.From = from;

                smtpClient.Send(message);

            }
        }

      
        public Task Execute(IJobExecutionContext context)
        {
            GlobalLogger.Logger.Debug("executing email job...");
            //this.SendEmail();
            return Task.CompletedTask;
        }
    }

}
