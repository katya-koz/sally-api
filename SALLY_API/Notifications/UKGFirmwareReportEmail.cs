using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace SALLY_API.Emails
{
    public class UKGFirmwareReportEmail: IDisposable
    {
        public string subject { get; set; } = "UKG Firmware Report";
        public string body { get; set; } = "This email contains the data for the current state of the firmware update and the current on site users who need a badge upgrade.";
        //should be read from an HTML File ideally
        public string recipient { get; set; }
        public List<Attachment> attachments { get; set; }

        public UKGFirmwareReportEmail (FileStreamResult Attachment, string recipient)
        {
            attachments = new List<Attachment>();

            // Extract the stream and other properties from FileStreamResult
            Stream fileStream = Attachment.FileStream;
            string fileName = Attachment.FileDownloadName ?? "Attachment"; // Default name if not provided
            this.recipient = recipient;
            // Create the attachment
            Attachment attachment = new Attachment(fileStream, fileName);

            // Add it to the attachments list
            attachments.Add(attachment);

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

                message.To.Add(recipient);
                message.From = from; 
                message.Body = body;
                message.Subject = subject;


                smtpClient.Send(message);

            }

        }
        public void Dispose()
        {

        }

    }
}
