using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SALLY_API.WebServices;

namespace SALLY_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class HandHygieneController : ControllerBase
    {
        private readonly APIService _apiService;
        public HandHygieneController(APIService apiService)
        {
            _apiService = apiService;
        }
        [HttpGet("testHH", Name = "Test_HH_Call")]
        public string Test_call()
        {
            return "test 1";


        }

        [HttpPost("uploadroles",Name ="Upload_Roles")]
        public void UploadRoles(List<string> roles)
        {
            using (HHWebOperations hh = new HHWebOperations())
            {
                hh.UploadRoles(roles);
            }
        }

        [HttpPost("uploadgroups", Name = "Upload_Groups")]
        public void UploadGroups(List<string> groups)
        {
            using (HHWebOperations hh = new HHWebOperations())
            {
                hh.UploadGroups(groups);
            }
        }


        //[HttpPost("CleanHandHygieneUsers", Name = "Clean_Hand_Hygiene_Users")]
        //public async Task<IActionResult> CleanHandHygieneUsers()
        //{
            

        //    await _apiService.FullCleanHandHygiene();

        //    return Ok();
        //}

    }
}

