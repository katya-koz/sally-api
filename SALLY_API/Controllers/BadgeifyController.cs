using Microsoft.AspNetCore.Mvc;
using SALLY_API.Entities;
using System.Text.Json;
using SALLY_API.WebServices;
using Microsoft.Data.SqlClient;
using System.Data;


namespace SALLY_API.Controllers
{

    public class BadgeifyEvent { 
        public ADUser Target { get; set; }
        public string OldBadge { get; set; }
        public string NewBadge { get; set; }    
        public string Actor { get; set; }

        public string ? HHAction { get; set; }
        public string ? ActivateAction  { get; set; }
        public bool ? HHActionSuccess { get; set; }
        public bool ? ActivateActionSuccess { get; set; }
    
    }

    //  [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class BadgeifyController : ControllerBase
    {
        private readonly APIService _apiService;
        private InMemoryQueueService _queueService;


        public BadgeifyController(APIService apiService, InMemoryQueueService queueService)
        {
            _apiService = apiService;
            _queueService = queueService;
        }

        [HttpGet("get-logs-dataset", Name ="GetLogsDataset")]
        public async Task<IActionResult> GetLogsDataset()
        {   
            try
            {
                string jsonResult =  _apiService.GetBadgeifyLogsFromDatabase();
                return Ok(jsonResult);
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error(ex.ToString());
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("testBadge", Name = "Test_B_Call")]
        public string Test_call()
        {
            return "test 1";


        }


        [HttpPost("usersearch", Name = "UserSearch")]
        public async Task<IActionResult> UserSearch([FromBody] string userSearchInput)
        {
            var response = await _apiService.ADUserSearchAsync(userSearchInput);

            return Ok(JsonSerializer.Serialize(response)); // Returns a list of JSON strings.
        }

        [HttpPost("badgesearch", Name = "BadgeSearch")]
        public async Task<IActionResult> BadgeSearch([FromBody] string badgeSearchInput)
        {
            var response = await _apiService.GetAvailableBadges(badgeSearchInput);

            // Return the list directly without calling ToString()
            return Ok(response);
        }
        [HttpPost("badgecheck", Name = "BadgeCheck")]
        public async Task<bool> CheckBadge([FromBody] string badge)
        {
            if (await _apiService.CheckBadge(badge))
            {
                return true;
            }
            return false;
        }

        [HttpPost("getaction", Name = "GetAction")]
        public async Task<string> GetAction([FromBody] ADUser userInput, string badgeID)
        {
            GlobalLogger.Logger.Debug(userInput.ToString() + " badgeid:" + badgeID);

            // Get the status of Activate and HH accounts
            var response = await _apiService.CheckBadgeifyAction(userInput);

            // Parse the response into Activate and HH statuses
            var statuses = response.Split(',');
            string activateStatus = statuses[0];
            string hhStatus = statuses[1];

            // Build the specific message
            string activateMessage = activateStatus == "create"
                ? $"You are about to create an Activate profile for {userInput.Firstname} {userInput.Lastname}."
                : $"You are about to update {userInput.Firstname} {userInput.Lastname}'s Activate profile.";

            string hhMessage = hhStatus == "create"
                ? $"You are about to create a Hand Hygiene profile for {userInput.Firstname} {userInput.Lastname}."
                : $"You are about to update {userInput.Firstname} {userInput.Lastname}'s Hand Hygiene profile.";

            return $"{activateMessage} {hhMessage} Please confirm with the staff member that this information is correct before submitting. Badge number: {badgeID}.";
        }

       
        [HttpPost("ADAuthentication", Name = "Badgeify_ADAuthentication")]
        public async Task<IActionResult> ADAuthentication()
        {
            if (!Request.Headers.ContainsKey("username") || !Request.Headers.ContainsKey("password"))
            {
                return BadRequest("Username and password headers are required.");
            }

            var username = Request.Headers["username"].ToString();
            var password = Request.Headers["password"].ToString();

            var response = await _apiService.ADAuthentication(username, password, "Badgeify");
            GlobalLogger.Logger.Debug(response);

            return Ok(response);
        }



        [HttpPost("upsertuser", Name = "UpsertUser")]
        public async Task<UpsertResult> UpsertUser([FromBody] BadgeifyEvent e)
        {
            // Check for duplicates in the "in-process" users
            if (_queueService.inProcessUsers.ContainsKey(e.Target.Username))
            {
                return new UpsertResult
                {
                    UnassignBadgeSuccess = false,
                    HHUserOperationSuccess = false,
                    HHUserOperation = "",
                    ActivateUserOperationSuccess = false,
                    ActivateUserOperation = "",
                    Message = "Request failed - the user you are trying to process is currently being modified on another device. Please try again later."
                };
            }

            if (_queueService.inProcessUsers.Values.Any(u => u.BadgeID == e.Target.BadgeID))
            {
                return new UpsertResult
                {
                    UnassignBadgeSuccess = false,
                    HHUserOperationSuccess = false,
                    HHUserOperation = "",
                    ActivateUserOperationSuccess = false,
                    ActivateUserOperation = "",
                    Message = "Request failed - the badge you are trying to assign is currently in use on another device. Please try again with a different badge."
                };
            }

            // Add to the "in-process" users
            if (!_queueService.inProcessUsers.TryAdd(e.Target.Username, e.Target))
            {
                return new UpsertResult
                {
                    UnassignBadgeSuccess = false,
                    HHUserOperationSuccess = false,
                    HHUserOperation = "",
                    ActivateUserOperationSuccess = false,
                    ActivateUserOperation = "",
                    Message = "Request failed."
                };
            }

            var completionSource = new TaskCompletionSource<UpsertResult>();
            _queueService.TaskCompletionSources[e.Target.Username] = completionSource;

            _queueService.UserQueue.Enqueue(e.Target);
            _queueService.QueueNotifier.Release();

            try
            {
                // Wait for the operation result
                UpsertResult result = await completionSource.Task;

                e.ActivateActionSuccess = result.ActivateUserOperationSuccess;
                e.HHActionSuccess = result.HHUserOperationSuccess;
                e.ActivateAction = result.ActivateUserOperation;
                e.HHAction = result.HHUserOperation;

                return result;
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Debug($"Error waiting for operation to complete: {ex.Message}");
                throw;
            }
            finally
            {
                // Cleanup the TaskCompletionSource from the dictionary
                    _queueService.TaskCompletionSources.TryRemove(e.Target.Username, out _);
                using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE")))
                {
                    try
                    {
                        sql.InsertBadgeifyUserLog(e); 
                    }
                    catch (SqlException ex) 
                    {
                        GlobalLogger.Logger.Error(ex.ToString());
                        
                    }
                }


            }


        }

    }
}

