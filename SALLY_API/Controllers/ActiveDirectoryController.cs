using Microsoft.AspNetCore.Mvc;


namespace SALLY_API.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class ActiveDirectoryController : ControllerBase
    {
        private readonly APIService _apiService;

        public ActiveDirectoryController(APIService apiService)
        {
            _apiService = apiService;
        }

        [HttpPost("load", Name = "load")]
        public async Task<IActionResult> UserSearch()
        {
            var response = await _apiService.LoadAD();

            return Ok(response);
        }


    }
}
