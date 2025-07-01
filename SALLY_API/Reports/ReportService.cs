using DocumentFormat.OpenXml.Bibliography;
using System.Data;
using System.Text.Json;

namespace SALLY_API.Reports
{
    public class ReportService : IDisposable
    {
        private readonly string _emailTemplatePath = "D:\\ENVFiles\\emailtemplates.json";
        public static readonly string emailTemplatePath = @"D:\\ENVFiles\\"; // testing
        private Dictionary<string, EmailTemplate> _emailTemplates = new Dictionary<string, EmailTemplate>();

        public Dictionary<string, EmailTemplate> EmailTemplates
        {
            get
            {
                string jsonContent = File.ReadAllText(_emailTemplatePath);
                var templateCollection = JsonSerializer.Deserialize<TemplateCollection>(jsonContent);
                _emailTemplates = templateCollection?.Templates ?? new Dictionary<string, EmailTemplate>();

                return _emailTemplates;
            }
        }

        

        public void DownloadOutdatedBadgeFirmwareReport(string fileDownloadLocation)
        {
            using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE_STAGING")))
            {
                DataSet dataset = sql.GetUKGReports();
                Report outdatedBadgeFirmwareReport = new OutdatedBadgeFirmwareReport(dataset);
                outdatedBadgeFirmwareReport.DownloadReport(fileDownloadLocation);
            }
        }
        public void EmailOutdatedBadgeFirmwareReport()
        {
            if (!EmailTemplates.TryGetValue("OutdatedBadgeFirmwareReport", out EmailTemplate emailTemplate))
            {
                throw new Exception("Email template for 'OutdatedBadgeFirmwareReport' not found.");
            }

            using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE_STAGING")))
            {
                DataSet summarydataset = sql.GetUKGReports();
                Report outdatedBadgeFirmwareReport = new OutdatedBadgeFirmwareReport(summarydataset);

                DataSet departmentdataset = sql.GetUKGDepartmentReports();
                Report outdatedBadgeFirmwareReportByDepartment = new OutdatedBadgeFirmwareReportByDepartment(departmentdataset);

                //DataSet Totalsdataset = sql.GetUKGDepartmentReportsTotal();
                //Report outdatedBadgeFirmwareReportByDepartmentTotal = new OutdatedBadgeFirmwareReportByDepartment(Totalsdataset);

                List<Report> attachments = new List<Report> { outdatedBadgeFirmwareReport, outdatedBadgeFirmwareReportByDepartment  };
               
                Email email = new Email(
                    attachments,
                    emailTemplate
                );

                email.SendEmail();
            }
        }

        public void DownloadEMTemperatureReport(string fileDownloadLocation, string department)
        {
          using (SQL sql = new SQL(Server.RTLS, "EM_TEST"))
            {
                var dataset = sql.GetEMDataByDepartment(department);
                Report EMTemperatureReport = new EMTemperatureReport(dataset,department);
                EMTemperatureReport.DownloadReport(fileDownloadLocation);
            
            }
           
        }

        public void EmailEMTemperatureReport() // this sends 2 emails, for ED and Pharm
        {
            if (!EmailTemplates.TryGetValue("EMTemperatureReport_Pharmacy", out EmailTemplate emailTemplate_pharm))
            {
                throw new Exception("Email template for 'EMTemperatureReport_Pharmacy' not found.");
            }

            if (!EmailTemplates.TryGetValue("EMTemperatureReport_ED", out EmailTemplate emailTemplate_ED))
            {
                throw new Exception("Email template for 'EMTemperatureReport_ED' not found.");
            }

            if (!EmailTemplates.TryGetValue("EMTemperatureReport_PUCC", out EmailTemplate emailTemplate_PUCC)) // postpartum and pucc
            {
                throw new Exception("Email template for 'EMTemperatureReport_PUCC' not found.");
            }
            GlobalLogger.Logger.Debug("Templates found");

            using (SQL sql = new SQL(Server.RTLS, Environment.GetEnvironmentVariable("EMDB")))
            {

                List<Report> attachments_pharm = GenerateAssociatedReports("EMTemperatureReport_Pharmacy");
                List<Report> attachments_ED = GenerateAssociatedReports("EMTemperatureReport_ED");
                List<Report> attachments_PUCC = GenerateAssociatedReports("EMTemperatureReport_PUCC");

                Email email_pharm = new Email(
                    attachments_pharm,
                    emailTemplate_pharm
                );

                email_pharm.SendEmail();

                Email email_ED = new Email(
                    attachments_ED,
                    emailTemplate_ED
                );

                email_ED.SendEmail();

                Email email_PUCC = new Email(
                    attachments_PUCC,
                    emailTemplate_PUCC
                );

                email_PUCC.SendEmail();
            }
            

        }
/*        public async Task SendBatteryReports()
        {
            if (!EmailTemplates.TryGetValue("Battery_Report", out EmailTemplate emailTemplate_Battery))
            {
                Console.WriteLine("Email template for 'IPAC_HH_ComplianceReport' not found");

                throw new Exception("Email template for 'IPAC_HH_ComplianceReport' not found.");
            }

            using (SQL sql = new SQL(Server.RTLS, "Battery"))
            {
                Console.WriteLine("Getting Data from battery");

                DataSet ds = sql.GetBatteries();

                BatteryReport batreport = new BatteryReport();

                Email email_battery = new Email(batreport, emailTemplate_Battery);
                email_battery.SendEmail();
            }

        }*/
        public async Task SendIPACReports()
        {
            try
            {

                Console.WriteLine("Begining IPAC Report");
                if (!EmailTemplates.TryGetValue("IPAC_HH_ComplianceReport", out EmailTemplate emailTemplate_IPAC))
                {
                    Console.WriteLine("Email template for 'IPAC_HH_ComplianceReport' not found");

                    throw new Exception("Email template for 'IPAC_HH_ComplianceReport' not found.");
                }
                Console.WriteLine("Making sql connection");

                using (SQL sql = new SQL(Server.BizTalkPoc1, Environment.GetEnvironmentVariable("BIZTALK_REPORTS")))
                {
                    Console.WriteLine("Getting Data from SQL");

                    DataSet ds = sql.GetIPACReport();
                    Console.WriteLine("Got data from sql");

                    IPACReport attachment = new IPACReport(ds);

                    Email email_ipac = new Email(
                            attachment, emailTemplate_IPAC

                        );
                    email_ipac.SendEmail();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IPACReport Constructor Error: {ex.Message}");
                throw;
            }

        }
        public async Task SendBatterySummaryReports()
        {
            try
            {

                Console.WriteLine("Begining Battery Report");
                if (!EmailTemplates.TryGetValue("Battery_SummaryReport", out EmailTemplate Battery_SummaryReport))
                {
                    Console.WriteLine("Email template for 'IPAC_HH_ComplianceReport' not found");

                    throw new Exception("Email template for 'IPAC_HH_ComplianceReport' not found.");
                }
                Console.WriteLine("Making sql connection");

                using (SQL sql = new SQL(Server.RTLS, Environment.GetEnvironmentVariable("BATTERY_DB")))
                {
                    Console.WriteLine("Getting Data from SQL");

                    DataSet ds = sql.GetBatteriesSummarReport();
                    Console.WriteLine("Got data from sql");

                    BatteryReport attachment = new BatteryReport(ds);

                    Email email_battery = new Email(
                            attachment, Battery_SummaryReport

                        );
                    email_battery.SendEmail();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IPACReport Constructor Error: {ex.Message}");
                throw;
            }

        }
        //don't do this just return te value, there is no reason that this is a void function 
        private void GetEMReportByDepartment(string[] departments, out List<Report> reports)
        {
            // Initialize reports before adding to it
            reports = new List<Report>();

            using (SQL sql = new SQL(Server.RTLS, Environment.GetEnvironmentVariable("EMDB")))
            {
                foreach (string dep in departments)
                {
                    DataSet ds = sql.GetEMDataByDepartment(dep);
                    reports.Add(new EMTemperatureReport(ds, dep)); // Add to the initialized list
                }
            }
        }

        public List<Report> GenerateAssociatedReports(string reportName)
        {
            List<Report> reports = new List<Report>();
            GlobalLogger.Logger.Debug("Generating report: " + reportName);
            switch (reportName)
            {
                case "EMTemperatureReport_Pharmacy":
                    // Pharmacy wants 4 reports
                    GetEMReportByDepartment(new string[] { "Pharmacy", "ED and DASA", "Mother & Baby PUCC", "Post-Partum DEF" }, out reports);
                    break;

                case "EMTemperatureReport_ED":
                    GetEMReportByDepartment(new string[] { "ED and DASA" }, out reports);
                    break;

                case "EMTemperatureReport_PUCC":
                    GetEMReportByDepartment(new string[] { "Mother & Baby PUCC", "Post-Partum DEF" }, out reports);
                    break;

                default:
                    GlobalLogger.Logger.Error("Attempted to generate reports for " + reportName + ", but the template was not found in email templates.");
                    break;
            }

            return reports;
        }



        public void Dispose()
        {
            GC.SuppressFinalize( this );
        }
    }

    public class EmailTemplate
    {
        public List<string> Recipients { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<string> CCRecipients { get; set; } = new List<string>();
        public List<string> BCCRecipients { get; set; } = new List<string>();

        public Dictionary<string, List<int>> Schedule { get; set; } = new Dictionary<string, List<int>>();

        public Dictionary<DayOfWeek, List<TimeSpan>> RecurringSchedule { get; set; } = new Dictionary<DayOfWeek, List<TimeSpan>>();

       
 
    }




    public class TemplateCollection
    {
        public Dictionary<string, EmailTemplate> Templates { get; set; }
    }
}
