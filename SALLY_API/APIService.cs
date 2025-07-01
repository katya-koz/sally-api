using SALLY_API.Reports;
using SALLY_API.WebServices;
using SALLY_API.Entities;
using Microsoft.AspNetCore.Mvc;
using SALLY_API.Controllers;
using SALLY_API.Entities.Handsify;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Bibliography;
using Newtonsoft.Json;
using System.Data;
using System.Text;
namespace SALLY_API
{
    public static class VersionInfo
    {

        public static bool IsScheduler { get; set; }
        public static string EnvFilePath { get; set; }
        public static string LogFilePath { get; set; }

    }
    public enum ReportType
    {
        OutdatedBadges,
    }
    public enum UpsertStatus
    {
        Success,
        FailedToUnassignBadge,
        HHUpdateFailed,
        HHCreateFailed,
        ActivateUpdateFailed,
        ActivateCreateFailed,
        GeneralFailure
    }
    public class SyncUsersResult {
        public List<UpsertResult> SyncResults = new List<UpsertResult>();
        public string Summary;

        }
   

    public class APIService:IDisposable
    {
        private ADService _adService = new ADService();

        public async Task<string> ADAuthentication(string username, string password, string application)
        {

            if (application == "Handsify")
            {
                GlobalLogger.Logger.Debug("AD Auth for Handsify");
            }
            string response = await _adService.Authentication(username, password, application);

            GlobalLogger.Logger.Debug($"Trying to authenticate {username} for application {application}.\nRecieved response: {response}");

            return response;
        }


        public async Task<List<ADUser>> ADUserSearchAsync(string AD)
        {
            List<ADUser> users = new List<ADUser>();
           

            users = await _adService.ADUsersSearchAsyncWithIdeals(AD);

            return users;
        }

        public async Task SetStation(HHStation station, List<Note> newNotes, List<int> archivedNotes, int floor, string pod)
        {
            using (SQL sql = new SQL(Server.RTLS, Environment.GetEnvironmentVariable("HANDSIFY_DB")))
            {
                GlobalLogger.Logger.Debug("setting station, " + station.ToString() + " floor " + floor + " pod " + pod);

                try { sql.SaveStation(station, newNotes, archivedNotes, floor, pod); }
                catch (Exception ex)
                {
                    GlobalLogger.Logger.Error($"Error in SetStation: {ex.Message}");
                    throw new InvalidOperationException("An error occurred while setting the station.", ex);


                    }

            }
        }
        public  string GetBadgeifyLogsFromDatabase()
        {
            using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE")))
            {
                return  SerializeDataTable(sql.GetBadgeifyLogs());
            }
        }

        private string SerializeDataTable(DataTable dataTable)
        {
            List<Dictionary<string, object>> rowsList = new List<Dictionary<string, object>>();

            foreach (DataRow row in dataTable.Rows)
            {
                var rowDict = new Dictionary<string, object>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    rowDict[column.ColumnName] = row[column];
                }
                rowsList.Add(rowDict);
            }

            return System.Text.Json.JsonSerializer.Serialize(rowsList);
        }
        public async Task<int> LoadAD()
        {
            try
            {
                _adService.GetUsersAll();
                return 0;
            }
            catch { return 1; }
        }


        public async Task<string> DeleteUsers()
        {

            List<ADUser> users = new List<ADUser>();

            using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE")))
            {

                users = await sql.GetCleanUpDeleteADUsers();

            }
            int ucount = users.Count;
            Console.WriteLine(ucount);
            using (WebOperationService operations = await WebOperationService.CreateWebOperationServiceAsync())
            {

                foreach (ADUser user in users)
                {
                   await operations.DeleteUser(user);
                   
                }
            }
            using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE")))
            {
                await sql.ReLoadDB();
                users = await sql.GetCleanUpDeleteADUsers();
            }
            int deletedUsers = ucount-users.Count; 
                return $"Deleted {deletedUsers} users out of {ucount}.";

        }
        public async Task<SyncUsersResult> SyncUsers()
        {

            List<UpsertResult> statuses = new List<UpsertResult>();
            List<ADUser> users = new List<ADUser>(); // get from sql
            int hhFailures = 0;
            int activateFailiures = 0;

            using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE")))
            {

                users = await sql.GetCleanUpADUsers();

            }
               

            using (WebOperationService operations = await WebOperationService.CreateWebOperationServiceAsync())
            {
                
                foreach (ADUser user in users)
                {
                    UpsertResult response = await operations.UpsertUser(user);
                    statuses.Add(response);
                    Console.WriteLine(response.ToString());
                    if (!response.HHUserOperationSuccess)
                    {
                        hhFailures++;
                    }
                    if (!response.ActivateUserOperationSuccess) { activateFailiures++;}


                }


                string summary = $"HH FAILURES: {hhFailures}\nACTIVATE FAILURES: {activateFailiures}\n";

                SyncUsersResult syncResult = new SyncUsersResult(); 
                
                syncResult.Summary = summary;
                syncResult.SyncResults = statuses;

                return syncResult;
            }
        }

        public async Task<string> CheckBadgeifyAction(ADUser user)
        {
            using (SQL sql = new SQL(Server.Activate, Environment.GetEnvironmentVariable("Activate_DB")))
            {
                user.ActivateUser.ItemID = await sql.GetActivateItem(user.Username);
            }
            using (SQL sql = new SQL(Server.HH, Environment.GetEnvironmentVariable("HH_DB")))
            {
                user.HHUser.ItemID = await sql.GetHHItem(user.Username);
            }

            string activateStatus = user.ActivateUser.ItemID == null ? "create" : "update";
            string hhStatus = user.HHUser.ItemID == null ? "create" : "update";

            return $"{activateStatus},{hhStatus}";
        }
       
        /*
         * This function creates missing users in bulk for clean up not individual use 
         * 
         */
        //can be done multiple ways going with an easy to read one but could be done with fewer inputs 
        public async Task<UpsertResult> UpsertUser(ADUser user)
        {
            using (SQL sql = new SQL(Server.Activate, Environment.GetEnvironmentVariable("Activate_DB")))
            {
                try
                {
                    user.ActivateUser.ItemID = await sql.GetActivateItem(user.Username);
                }
                catch (Exception ex) {
                    GlobalLogger.Logger.Error("There was an error getting the Activate item id for the user: " + user.ToString());
                }

            }
            using (SQL sql = new SQL(Server.HH, Environment.GetEnvironmentVariable("HH_DB")))
            {
                try
                {
                    user.HHUser.ItemID = await sql.GetHHItem(user.Username);
                }
                catch (Exception ex)
                {
                    GlobalLogger.Logger.Error("There was an error getting the Hand Hygiene item id the user: " + user.ToString());
                }
            }

            using (WebOperationService operations = await WebOperationService.CreateWebOperationServiceAsync())
            {


                var response = await operations.UpsertUser(user);
                return response;
            }

        }

        public async Task DownloadOutdatedBadgeFirmwareReport(string FileDownloadLocation)
        {
            using (ReportService reports = new ReportService())
            {
                reports.DownloadOutdatedBadgeFirmwareReport(FileDownloadLocation);
            }
        }
        public async Task EmailOutdatedBadgeFirmwareReport()
        {
            using (ReportService reports = new ReportService())
            {
                reports.EmailOutdatedBadgeFirmwareReport();
            }
        }


        /*EM TEMP REPORTS*/
        public async Task DownloadEMTemperatureReport(string FileDownloadLocation, string department)
        {
            using (ReportService reports = new ReportService())
            {
                reports.DownloadEMTemperatureReport(FileDownloadLocation, department);
            }
        }
        public async Task EmailEMTemperatureReport()
        {
            using (ReportService reports = new ReportService())
            {
                reports.EmailEMTemperatureReport();
            }
        }



        public async Task LoadPulseTagReport()
        {
            using (PulseWebOperations pulse = new PulseWebOperations(Site.CVH))
            {
                string report = await pulse.DownloadFirmwareReport(Device.badge, Battery.All);
                using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE_STAGING")))
                {
                    await sql.InsertFirmwareReport(report);
                }
            }
        }

        public async Task LoadPulseDeadTagReport()
        {
            using (PulseWebOperations pulse = new PulseWebOperations(Site.CVH))
            {
                string report = await pulse.DownloadFirmwareReport(Device.badge, Battery.Dead);
                string report2 = await pulse.DownloadFirmwareReport(Device.badge, Battery.Critical);
                using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE_STAGING")))
                {
                    await sql.InsertDeadBatteryReport(report);
                    await sql.InsertDeadBatteryReport(report2); 
                }
            }

        }

        public async Task LoadPulseHHReport()
        {
            using (PulseWebOperations pulse = new PulseWebOperations(Site.CVH))
            {
                string report = await pulse.DownloadHHStationFirmwareReport();
                using (SQL sql = new SQL(Server.RTLS, Environment.GetEnvironmentVariable("RTLS_STAGING")))
                {
                    await sql.InsertStationReport(report);
                }
            }
        }

       
        public async Task<bool> CheckBadge(string badge)
        {
            using (SQL sql = new SQL(Server.Activate, Environment.GetEnvironmentVariable("Activate_DB")))
            {
                if (sql.CheckBadge(badge).Equals("ERROR"))
                {
                    return false;
                }


            }
            return true;
        }
        public async Task<List<int>> GetAvailableBadges(string badgenumber)
        {
            List<int> badges = new List<int>();
            try
            {
                using (SQL sql = new SQL(Server.Activate, Environment.GetEnvironmentVariable("Activate_DB")))
                {
                    badges = sql.GetBadges(int.Parse(badgenumber));
                }
            }
            catch (Exception ex) {
                GlobalLogger.Logger.Error(ex.ToString());
            }
            return badges;
        }

        public async Task<Pod> GetPod(string floor, string unit)
        {
            try
            {
                Pod pod = new Pod();
            using (SQL sql = new SQL(Server.RTLS, Environment.GetEnvironmentVariable("HANDSIFY_DB")))
            {
   

                
                    pod = await sql.GetPod(floor, unit);
                    GlobalLogger.Logger.Debug("these are the stations returend: " );
                    foreach (HHStation s in pod.HHStations.Values) {
                        GlobalLogger.Logger.Debug(s.ToString());
                    
                    }
               
                return pod;
        }

            }
            catch (Exception ex)
            {

                GlobalLogger.Logger.Error(ex.ToString());
            }
            return null;
        }

        public async Task<Pod> GetOperationalPod(string floor, string unit)
        {
            try
            {
                Pod pod = new Pod();
                using (SQL sql = new SQL(Server.RTLS, Environment.GetEnvironmentVariable("HANDSIFY_DB")))
                {
              


                    pod = await sql.GetOperationalPod(floor, unit);


                    return pod;
                }

            }
            catch (Exception ex)
            {

                GlobalLogger.Logger.Error(ex.ToString());
            }
            return null;
        }

        public async Task<string> ArchiveStation(string stationKey)
        {
            try
            {
                using (SQL sql = new SQL(Server.RTLS, Environment.GetEnvironmentVariable("HANDSIFY_DB")))
                {
                    await sql.ArchiveStation(stationKey);
                    return $"Successfully archived station with key: {stationKey}";
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error(ex.ToString());
                return $"Failed to archive station: {ex.Message}";
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

    }
}