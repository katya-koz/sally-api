using System.Net;
using System.Text;
using HtmlAgilityPack;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace SALLY_API.WebServices
{
    internal enum Site 
    {
    CVH,
    MRH 
    
    }
    internal enum Device
    {
        tagmicro,
        tagmultimode,
        HHStation,
        badge,
        PatientTag,
        BLE,
        TempSensor

    }

    internal enum Battery
    {
        All,
        Good,
        Low,
        Critical,
        Dead

    }
    internal class PulseWebOperations: IDisposable
    {
        private string username { get; set; }
        private string password { get; set; }
        private CookieContainer cookiebox { get; set; }
        private HttpClientHandler handler { get; set; }
        private UriBuilder url;

        private string ViewStateGenerator;

        private HttpClient httpClient { get; set; }


        internal PulseWebOperations(Site site)        
        {
            try
            {
                username = Environment.GetEnvironmentVariable("PULSE_USER");
                password = Environment.GetEnvironmentVariable("PULSE_PASS");
                cookiebox = new CookieContainer();
                
                handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = true,
                    CookieContainer = cookiebox,
                    UseCookies = true,
                    MaxAutomaticRedirections = 1000 // Adjust as needed

                };
                httpClient = new HttpClient(handler);
                url = new UriBuilder(Environment.GetEnvironmentVariable("PULSE_ROOT_URL") +"/"+GetSite(site));
                httpClient.BaseAddress = url.Uri;
                httpClient.Timeout = TimeSpan.FromSeconds(20); // Set a reasonable timeout
                httpClient.DefaultRequestHeaders.ExpectContinue = false;
                httpClient.DefaultRequestHeaders.ConnectionClose = true;
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
                httpClient.DefaultRequestHeaders.Add("Origin", httpClient.BaseAddress.ToString());

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Error initializing PulseWebOperations: {ex.Message}");
            }


        }

        private string GetSite(Site site)
        {
            switch (site)
            {
                case Site.CVH: 
                    ViewStateGenerator = "1ACB94BB";
                    return Environment.GetEnvironmentVariable("PULSE_CVH_SUBURL");

                case Site.MRH:
                    ViewStateGenerator = "AF3C0D0B";
                    return Environment.GetEnvironmentVariable("PULSE_MRH_SUBURL");
                default:
                    throw new ArgumentException("Server does not exist or has not been implemented yet.");


            }
        }
        internal async Task Login()
        {
            try

            {

                var loginPage = await httpClient.GetAsync("https://gms.centrak.com/gms3web/login.aspx");
                string html = await loginPage.Content.ReadAsStringAsync();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                //need to parse the html and find this 
                var node = doc.DocumentNode.SelectSingleNode("/html/body/form/div[1]/input");
                string VIEWSTATE = node.GetAttributeValue("value", "");
                string encryptedpassword = Encrypt(password, "8080808080808080", "8080808080808080");
                var keyValuePairs = new List<KeyValuePair<string, string>>
                {

                new KeyValuePair<string, string>("__VIEWSTATE",VIEWSTATE),
                new KeyValuePair<string, string>("__VIEWSTATEGENERATOR", ViewStateGenerator),
                new KeyValuePair<string, string>("__VIEWSTATEENCRYPTED", ""),
                new KeyValuePair<string, string>("txtUsername", username),
                new KeyValuePair<string, string>("hdnPassword", encryptedpassword),
                new KeyValuePair<string, string>("btnLogin", "Login")
                };
                var content = new FormUrlEncodedContent(keyValuePairs);

                var response = await httpClient.PostAsync("https://gms.centrak.com/gms3web/login.aspx", content);
                response.EnsureSuccessStatusCode();
            }
        
            catch (HttpRequestException httpEx)
            {
                GlobalLogger.Logger.Error($"HTTP error during login: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Unexpected error during login: {ex.Message}");
            }
        }

        public string GetBattery(Battery battery)
        {
            switch (battery)
            {
                case Battery.All:
                    return "";
                case Battery.Good:
                    return "0";
                case Battery.Low:
                    return "1";
                case Battery.Critical:
                    return "2";
                case Battery.Dead:
                    return "7";
                default:
                    return "Unknown battery status";
            }


        }
        internal async Task<string> DownloadFirmwareReport(Device device, Battery battery)
        {
            await Login();
            try
            {
                string status= GetBattery(battery);
                var keyValuePairs = GetPulseQuery(device, status);


                var content = new FormUrlEncodedContent(keyValuePairs);

                var response = await httpClient.PostAsync("https://gms.centrak.com/gms3web/AjaxConnector.aspx?cmd=DownloadExcel", content);
                response.EnsureSuccessStatusCode();
                string csvresponse = await response.Content.ReadAsStringAsync();
                return csvresponse;
            }

            catch (HttpRequestException httpEx)
            {
                GlobalLogger.Logger.Error($"HTTP error during firmware download: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Unexpected error during firmware download: {ex.Message}");
            }
         return "ERROR";

        }

        private List<KeyValuePair<string,string>> GetPulseQuery(Device device, string battery)
        {
            var keyvaluepair = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("sid", "1076"),
                new KeyValuePair<string, string>("alertId", ""),
                new KeyValuePair<string, string>("Bin", battery),
                new KeyValuePair<string, string>("curpage", "0"),
                new KeyValuePair<string, string>("DeviceId", ""),
                new KeyValuePair<string, string>("devicetype", "1"),
                new KeyValuePair<string, string>("sorColumnname", "LastSeen"),
                new KeyValuePair<string, string>("SorOrder", "desc"),
                new KeyValuePair<string, string>("g_LTCsite", "0")

            };
            //add query payloads here 
            switch (device)
            {
                case Device.badge:
                     keyvaluepair.Append(new KeyValuePair<string, string>("typId", "30"));
                    keyvaluepair.Append(new KeyValuePair<string, string>("devicetype", "1"));
                    return keyvaluepair;

                case Device.tagmicro:
                    keyvaluepair.Append(new KeyValuePair<string, string>("typId", "4"));
                    keyvaluepair.Append(new KeyValuePair<string, string>("devicetype", "1"));

                    return keyvaluepair;

                case Device.tagmultimode:
                    keyvaluepair.Append(new KeyValuePair<string, string>("typId", "7"));
                    keyvaluepair.Append(new KeyValuePair<string, string>("devicetype", "1"));

                    return keyvaluepair;
                case Device.HHStation:
                    keyvaluepair.Append(new KeyValuePair<string, string>("typId", "2"));
                    keyvaluepair.Append(new KeyValuePair<string, string>("devicetype", "2"));

                    return keyvaluepair;


                default:

                    throw new ArgumentException("Invalid or unimplemented device");
            }
        }

        internal async Task<string> DownloadHHStationFirmwareReport()
        {
            await Login();
            try
            {
                var keyValuePairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("sid", "1076"),
                new KeyValuePair<string, string>("alertId", ""),
                new KeyValuePair<string, string>("Bin", ""),
                new KeyValuePair<string, string>("curpage", "0"),
                new KeyValuePair<string, string>("typId", "2"),
                new KeyValuePair<string, string>("DeviceId", ""),
                new KeyValuePair<string, string>("devicetype", "2"),
                new KeyValuePair<string, string>("sorColumnname", "LastSeen"),
                new KeyValuePair<string, string>("SorOrder", "desc"),
                new KeyValuePair<string, string>("g_LTCsite", "0")
            };

                var content = new FormUrlEncodedContent(keyValuePairs);

                var response = await httpClient.PostAsync("https://gms.centrak.com/gms3web/AjaxConnector.aspx?cmd=DownloadExcel", content);
                response.EnsureSuccessStatusCode();
                string csvresponse = await response.Content.ReadAsStringAsync();

                return csvresponse;
            }

            catch (HttpRequestException httpEx)
            {
                GlobalLogger.Logger.Error($"HTTP error during firmware download: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Unexpected error during firmware download: {ex.Message}");
            }
            return "ERROR";

        }


        public static string Encrypt(string plainText, string key, string iv)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Error during disposal: {ex.Message}");
            }
        }
    }
}
