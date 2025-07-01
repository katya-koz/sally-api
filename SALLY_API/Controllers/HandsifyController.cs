using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SALLY_API.WebServices;
using System.Text.Json;
using SALLY_API.Entities.Handsify;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Spreadsheet;


namespace SALLY_API.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class HandsifyController : ControllerBase
    {

        private readonly APIService _apiService;
        private InMemoryQueueService _queueService;


        public HandsifyController(APIService apiService, InMemoryQueueService queueService)
        {
            _apiService = apiService;
            _queueService = queueService;
        }

        [HttpPost("get-pod", Name = "GetPod")]
        public async Task<IActionResult> GetPod()
        {
            string floor;
            string unit;

            if (!Request.Headers.ContainsKey("floor") || !Request.Headers.ContainsKey("unit"))
            {
                //floor = 5;
                //unit = 'C';
                return BadRequest("A floor and a unit code is required.");
            }
            else
            {
                floor = Request.Headers["floor"].ToString();
                unit = Request.Headers["unit"].ToString();

            }


            var response = await _apiService.GetPod(floor, unit);

            return Ok(JsonConvert.SerializeObject(response));
            //    //all the stations
        }

        [HttpPost("get-operational-pod", Name = "GetOperationalPod")]
        public async Task<IActionResult> GetOperationalPod()
        {
            string floor;
            string unit;

            if (!Request.Headers.ContainsKey("floor") || !Request.Headers.ContainsKey("unit"))
            {
                return BadRequest("A floor and a unit code is required.");
            }
            else
            {
                floor = Request.Headers["floor"].ToString();
                unit = Request.Headers["unit"].ToString();

            }

            var response = await _apiService.GetOperationalPod(floor, unit);

            return Ok(JsonConvert.SerializeObject(response));
        }




        [HttpGet("archive-station", Name = "ArchiveStation")]
        public async Task<IActionResult> ArchiveStation()
        {
            string stationKey;
            if (!Request.Headers.ContainsKey("stationKey"))
            {
                return BadRequest("A station key is required.");
            }
            else
            {
                stationKey = Request.Headers["stationKey"].ToString();


            }


            var response = await _apiService.ArchiveStation(stationKey);

            return Ok(response);
        }
        public class StationRequest
        {
            public HHStation Station { get; set; }
            public int Floor { get; set; }
            public string Pod { get; set; }
            public List<Note> NewNotes { get; set; }
            public List<int> ArchivedNotes { get; set; }
        }

        [HttpPost("SetStation", Name = "SetStation")]
        public async Task<IActionResult> SetStation([FromBody] StationRequest request)
        {
            GlobalLogger.Logger.Debug("setting station");

            if (request?.Station == null)
            {
                return BadRequest("Invalid request body.");
            }

            try
            {
                await _apiService.SetStation(request.Station, request.NewNotes, request.ArchivedNotes, request.Floor, request.Pod);
                return Ok();

            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error("error in setStation Post: " + ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("ADAuthentication", Name = "Handsify_ADAuthentication")]
        public async Task<IActionResult> ADAuthentication()
        {

            GlobalLogger.Logger.Debug("AD Authentication for Handisfy Began");
            // Extract username and password from request headers
            if (!Request.Headers.ContainsKey("username") || !Request.Headers.ContainsKey("password"))
            {
                GlobalLogger.Logger.Debug("AD Authentication for Handisfy Began");

                return BadRequest("Username and password headers are required.");
            }

            var username = Request.Headers["username"].ToString();
            var password = Request.Headers["password"].ToString();
            var response = await _apiService.ADAuthentication(username, password, "Handsify");
            GlobalLogger.Logger.Debug(response);
            return Ok(response);
        }
    }
}
