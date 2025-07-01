using Azure;
using Microsoft.AspNetCore.Mvc;
using SALLY_API.Reports;

namespace SALLY_API.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class ReportsController : ControllerBase
    {
        private readonly APIService _apiService;

        public ReportsController(APIService apiService)
        {
            _apiService = apiService;
        }



        [HttpPost("UploadFirmwareReport", Name = "Upload_Firmware_Report")]
        public async Task<IActionResult> UploadFirmware()
        {
            await _apiService.LoadPulseTagReport();

            return Ok();
        }

        [HttpPost("UploadHHFirmwareReport", Name = "Upload_HH_Firmware_Report")]
        public async Task<IActionResult> UploadHHFirmware()
        {
            await _apiService.LoadPulseHHReport();

            return Ok();
        }

        [HttpPost("UploadLowBatteryTagReport", Name = "Upload_Low_Battery_Tag_Report")]
        public async Task<IActionResult> UploadLowBattery()
        {
            await _apiService.LoadPulseDeadTagReport();
            return Ok();
        }


        [HttpGet("DownloadFirmwareReport", Name = "Download_Firmware_Report")]
        public async Task<IActionResult> DownloadFirmwareReport(string fileDownloadLocation = "D:\\Report Downloads\\Outdated Firmware Badge Reports")
        {

            try {

                _apiService.DownloadOutdatedBadgeFirmwareReport(fileDownloadLocation);

                return Ok(new { Message = "Outdated badge firmware report download successful", DownloadLocation = fileDownloadLocation});
            }

            catch (Exception ex)
            {
                // Handle exceptions and return error response
                return StatusCode(500, new { Message = "An error occurred during the download", Error = ex.Message });
            }

        }



        [HttpGet("EmailFirmwareReport", Name = "Email_Firmware_Report")]
        public async Task<IActionResult> EmailFirmwareReport()
        {
            try
            {
                await _apiService.EmailOutdatedBadgeFirmwareReport();

                return Ok(new { Message = "Outdated badge firmware report email successful"});
            }

            catch (Exception ex)
            {
                // Handle exceptions and return error response
                return StatusCode(500, new { Message = "An error occurred during the email", Error = ex.Message });
            }

        }



        /*EMTEMPERATURE REPORTS*/

        [HttpGet("DownloadEMTemperatureReport", Name = "Download_EMTemperature_Report")]
        public async Task<IActionResult> DownloadEMTemperatureReport(string department, string fileDownloadLocation = "D:\\Report Downloads\\EMTemperature Reports")
        {

            try
            {

              await  _apiService.DownloadEMTemperatureReport(fileDownloadLocation, department);

                return Ok(new { Message = "EMTemperature report download successful", DownloadLocation = fileDownloadLocation });
            }

            catch (Exception ex)
            {
                // Handle exceptions and return error response
                return StatusCode(500, new { Message = "An error occurred during the download", Error = ex.Message });
            }

        }

        [HttpGet("EmailEMTemperatureReport", Name = "Email_EMTemperature_Report")]
        public async Task<IActionResult> EmailEMTemperatureReport()
        {
            try
            {
                await _apiService.EmailEMTemperatureReport();

                return Ok(new { Message = "EMTemperature report email successful" });
            }

            catch (Exception ex)
            {
                // Handle exceptions and return error response
                return StatusCode(500, new { Message = "An error occurred during the email", Error = ex.Message });
            }

        }


    }
}
