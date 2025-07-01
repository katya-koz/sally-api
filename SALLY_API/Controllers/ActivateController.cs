using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SALLY_API.WebServices;

namespace SALLY_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ActivateController : ControllerBase
    {


        [HttpPost("uploaddepartments", Name = "Upload_Departments")]
        public void UploadDepartments(List<string> deps)
        {
            using (ActivateWebOperations a = new ActivateWebOperations())
            {
                a.UploadDepartments(deps);
            }
        }

        [HttpPost("uploadgroupsAA", Name = "Upload_GroupsAA")]
        public void UploadGroups(List<string> groups)
        {
            using (ActivateWebOperations a = new ActivateWebOperations())
            {
               a.UploadGroups(groups);
            }
        }
    }
}
