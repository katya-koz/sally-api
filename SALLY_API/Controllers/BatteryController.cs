using Microsoft.AspNetCore.Mvc;

namespace SALLY_API.Controllers
{
    public class BatteryController : Controller
    {
        private readonly APIService _apiService;
        public BatteryController()
        {
            _apiService = new APIService();
        }
        public async Task<IActionResult> LoadUKGSchedule()
        {

     //       await _apiService.LoadUKGReport();
            
            return View();
        }
    }
}
