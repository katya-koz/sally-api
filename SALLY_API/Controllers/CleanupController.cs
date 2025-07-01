using Microsoft.AspNetCore.Mvc;
using SALLY_API.Entities;
using Newtonsoft.Json;
using System.Text.Json;
using SALLY_API.WebServices;

namespace SALLY_API.Controllers
{
   
    //  [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class CleanupController : Controller
    {
        private readonly APIService _apiService;
        public CleanupController(APIService apiService)
        {
            _apiService = apiService;
        }
        [HttpPost("sync-users", Name = "SyncUsers")]
        public async Task<JsonResult> SyncUsers()
        {

           SyncUsersResult syncResults = await _apiService.SyncUsers();

            return Json(syncResults);

            //string summary = $"Synced {results.Count} users.";

            //return Json(new
            //{
            //    SyncStatuses = results,
            //    SyncSummary = summary
            //});
        }

        [HttpPost("delete-inactive-users", Name = "DeleteInactiveUsers")]
        public async Task<JsonResult> DeleteInactiveUsers()
        {

            string result = await _apiService.DeleteUsers();

            return Json(result);

            //string summary = $"Synced {results.Count} users.";

            //return Json(new
            //{
            //    SyncStatuses = results,
            //    SyncSummary = summary
            //});
        }

        [HttpGet("get-version")]
        public async Task<string> GetVersion()
        {
            // this will jsut confirm which environment the program is running in: dev, test, or prod
            return ($"Environment:  {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}\n" +
                $"Log File Path: {VersionInfo.LogFilePath}\n" +
                $"Environment File Path: {VersionInfo.EnvFilePath}\n" +
                $"Scheduler Running: {VersionInfo.IsScheduler}\n");


        }


    }
}
