using DocumentFormat.OpenXml.Drawing.Charts;
using SALLY_API.Entities;
using SALLY_API.Interfaces;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using static SALLY_API.WebServices.HHWebOperations;

namespace SALLY_API.WebServices
{
    internal class ActivateWebOperations : IWebOperations, IDisposable
    {
        private string authenticity_token { get; set; }
        private string username { get; set; }
        private string password { get; set; }
        private CookieContainer cookiebox { get; set; }
        private HttpClientHandler handler { get; set; }

        private UriBuilder url = new UriBuilder(Environment.GetEnvironmentVariable("ACTIVATE_ROOT_URL"));

        private HttpClient httpClient { get; set; }
        public ActivateWebOperations()
        {
            try
            {
                username = "";
                password = "";
                cookiebox = new CookieContainer();

                handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = true,
                    CookieContainer = cookiebox,
                    UseCookies = true,
                };
                httpClient = new HttpClient(handler);
                httpClient.BaseAddress = url.Uri;
                httpClient.Timeout = TimeSpan.FromSeconds(20); // Set a reasonable timeout
                httpClient.DefaultRequestHeaders.ExpectContinue = false;
                httpClient.DefaultRequestHeaders.ConnectionClose = true;
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
                httpClient.DefaultRequestHeaders.Referrer = new Uri($"{httpClient.BaseAddress}login");
                httpClient.DefaultRequestHeaders.Add("Origin", httpClient.BaseAddress.ToString());

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Error initializing ActivateWebOperations: {ex.Message}");
            }
        }

        public async Task<UpsertStatus> PerformUserProfileAction(ADUser user, HTTPRequestModifier requestModifier)
        {
            try
            {

                //await Login(); // allegedly, we should already be logged in!
                var response = await httpClient.GetAsync("/items");
                response.EnsureSuccessStatusCode();
                var payload = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("record[title]", user.Name),
            new KeyValuePair<string, string>("record[description]", user.ActivateUser.Role),
            new KeyValuePair<string, string>("record[system_id]", user.Username + "-AD"),
            new KeyValuePair<string, string>("record[line_number]", user.Username),
            new KeyValuePair<string, string>("record[tag]", user.BadgeID),
            new KeyValuePair<string, string>("record[department]", $"{user.ActivateUser.DepartmentKey}"),
            new KeyValuePair<string, string>("record[item_sets][]", ""),
        };

                foreach (var group in user.ActivateUser.GroupKeys)
                {
                    payload.Add(new KeyValuePair<string, string>("record[item_sets][]", group.ToString()));
                }

                var content = new FormUrlEncodedContent(payload);
                await requestModifier(payload, user);

                var validation = await ValidateAction(user);
                return validation;
            }
            catch (HttpRequestException httpEx)
            {
                GlobalLogger.Logger.Error($"HTTP error in PerformUserProfileAction: {httpEx.Message}");
                return UpsertStatus.ActivateUpdateFailed;
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Unexpected error in PerformUserProfileAction: {ex.Message}");
                return UpsertStatus.ActivateUpdateFailed;
            }
        }

        public async Task Delete(ADUser user)
        {

            try
            {
                Console.WriteLine($"/items/{user.ActivateUser.ItemID}/discard");
                httpClient.PutAsync($"/items/{user.ActivateUser.ItemID}/discard", null);
                GlobalLogger.Logger.Debug($"Activate delete request sent for user {user.Username}");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Error deleting user {user.BadgeID}: {ex.Message}");
            }
        }

        public async Task<UpsertStatus> Create(ADUser user)
        {
            try
            {
                //GlobalLogger.Logger.Debug("ACTIVATE CREATE");
                var result = await PerformUserProfileAction(user, HTTPCreateModifier);
                GlobalLogger.Logger.Debug($"Create action completed for user {user.BadgeID}");
                return result;
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Error creating user {user.BadgeID}: {ex.Message}");
                throw new Exception($"Error creating user {user.BadgeID}: {ex.Message}");
            }
        }

        public async Task<UpsertStatus> Update(ADUser user)
        {
            try
            {
                //GlobalLogger.Logger.Debug("ACTIVATE UPDATE");
                var result =await PerformUserProfileAction(user, HTTPUpdateModifier);
                GlobalLogger.Logger.Debug($"Update action completed for user with badge: {user.BadgeID}");
                return result;
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Error updating user {user.BadgeID}: {ex.Message}");
                throw new Exception($"Error updating user {user.BadgeID}: {ex.Message}");
            }
        }

        private async Task HTTPUpdateModifier(List<KeyValuePair<string, string>> payload, ADUser user)
        {
            GlobalLogger.Logger.Debug("************ACTIVATE UPDATE***************");
            GlobalLogger.Logger.Debug(user.ToString());
            try
            {
                payload.Add(new KeyValuePair<string, string>("commit", "Update"));
                payload.Add(new KeyValuePair<string, string>("_method", "patch"));

                var content = new FormUrlEncodedContent(payload);
                Console.WriteLine($"/items/{user.ActivateUser.ItemID}");

                var post = await httpClient.PostAsync($"/items/{user.ActivateUser.ItemID}", content);
                post.EnsureSuccessStatusCode();
                //GlobalLogger.Logger.Debug($"Update HTTP modifier succeeded for user {user.BadgeID}");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Debug($"Error in HTTPUpdateModifier for user {user.BadgeID}: {ex.Message}");
            }
        }

        private async Task HTTPCreateModifier(List<KeyValuePair<string, string>> payload, ADUser user)
        {
            GlobalLogger.Logger.Debug("************ACTIVATE CREATE***************");
            GlobalLogger.Logger.Debug(user.ToString());
            try
            {
                payload.Add(new KeyValuePair<string, string>("commit", "Create"));

                var content = new FormUrlEncodedContent(payload);
                var post = await httpClient.PostAsync("/items", content);
                post.EnsureSuccessStatusCode();
                //GlobalLogger.Logger.Debug($"Create HTTP modifier succeeded for user {user.BadgeID}");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Error in HTTPCreateModifier for user {user.BadgeID}: {ex.Message}");
            }
        }

        // Additional logging and error handling are added similarly to other methods.

        public async Task Login()
        {
            try
            {
                var tokenResponse = await httpClient.GetAsync("/login");
                tokenResponse.EnsureSuccessStatusCode();
                authenticity_token = await GetToken(tokenResponse);

                var keyValuePairs = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("utf8", "✓"),
            new KeyValuePair<string, string>("authenticity_token", authenticity_token),
            new KeyValuePair<string, string>("name", Environment.GetEnvironmentVariable("ACTIVATE_USERNAME")),
            new KeyValuePair<string, string>("password", Environment.GetEnvironmentVariable("ACTIVATE_PASSWORD")),
            new KeyValuePair<string, string>("flash_version", ""),
            new KeyValuePair<string, string>("commit", "Login")
        };

                var content = new FormUrlEncodedContent(keyValuePairs);

                var response = await httpClient.PostAsync("login", content);
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

        private async Task<string> GetToken(HttpResponseMessage response)
        {
            try
            {
                var htmlContent = await response.Content.ReadAsStringAsync();
                var csrfTokenRegex = new Regex(@"<input\s+type=""hidden""\s+name=""authenticity_token""\s+value=""(.*?)""", RegexOptions.IgnoreCase);
                var match = csrfTokenRegex.Match(htmlContent);

                if (!match.Success)
                {
                    GlobalLogger.Logger.Debug("CSRF token not found in the page.");
                    return "";
                }

                return match.Groups[1].Value;
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Debug($"Error extracting token: {ex.Message}");
                return "";
            }
        }

        private void LogValidation(ADUser ideal, ADUser actual)
        {
            int[] idealGroupKeysSorted = ideal.ActivateUser.GroupKeys.OrderBy(n => n).ToArray();
            int[] actualGroupKeysSorted = actual.ActivateUser.GroupKeys.OrderBy(n => n).ToArray();
            int max = Math.Max(idealGroupKeysSorted.Length, actualGroupKeysSorted.Length);

            var sb = new StringBuilder();
            sb.AppendLine("Validation Results:");
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "Field", "Ideal", "Actual"));
            sb.AppendLine(new string('-', 80));
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "Name:", $"{ideal.Firstname} {ideal.Lastname}", $"{actual.Firstname} {actual.Lastname}"));
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "Username:", ideal.Username, actual.Username));
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "ItemId:", ideal.ActivateUser.ItemID, actual.ActivateUser.ItemID));
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "Role:", ideal.ActivateUser.Role, actual.ActivateUser.Role));
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "DepartmentKey:", ideal.ActivateUser.DepartmentKey, actual.ActivateUser.DepartmentKey));
            sb.AppendLine(string.Format("{0,-20}", "GroupKeys:"));


            for (int i = 0; i < max; i++)
            {
                string idealKey = i < idealGroupKeysSorted.Length ? idealGroupKeysSorted[i].ToString() : "";
                string actualKey = i < actualGroupKeysSorted.Length ? actualGroupKeysSorted[i].ToString() : "";

                sb.AppendLine(string.Format("{0,-20} {1,-60}", "",$"{idealKey}", $"{actualKey}"));
            }

            GlobalLogger.Logger.Debug(sb.ToString());
        }

        private async Task<UpsertStatus> ValidateAction(ADUser ideal)
        {
            ADUser actual = new ADUser();
            try
            {
                using (SQL sql = new SQL(Server.Activate, Environment.GetEnvironmentVariable("Activate_DB")))
                {
                    actual = sql.SearchUserFromActivate(ideal.BadgeID);
                }

                GlobalLogger.Logger.Debug("Performing validation step for Activate user: ");

                LogValidation(ideal,actual);

                if (ideal.Username == actual.Username && ideal.ActivateUser.IsEqualTo(actual.ActivateUser))
                {
                    GlobalLogger.Logger.Debug("Activate action validation successful for user: " + ideal.Username + " with badge: " + ideal.BadgeID);
                    return UpsertStatus.Success;
                }
                GlobalLogger.Logger.Debug("Activate action validation failed for user: " + ideal.Username + " with badge: " + ideal.BadgeID);
                return UpsertStatus.ActivateUpdateFailed;
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Activate action validation error: {ex.Message}");
                return UpsertStatus.ActivateUpdateFailed;
            }
        }

        public async Task UploadDepartments(List<string> departments)
        {
            try
            {
                await Login();
                string url = "http://172.25.111.232/departments";

                foreach (string dep in departments)
                {
                    var payload = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("record[title]", dep),
                new KeyValuePair<string, string>("commit", "Create"),
            };

                    var content = new FormUrlEncodedContent(payload);
                    var post = await httpClient.PostAsync(url, content);
                    post.EnsureSuccessStatusCode();
                    GlobalLogger.Logger.Debug($"Department {dep} uploaded successfully.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                GlobalLogger.Logger.Error($"HTTP error during department upload: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Unexpected error during department upload: {ex.Message}");
            }
        }

        public async Task UploadGroups(List<string> groups)
        {
            try
            {
                await Login();
                string url = "http://172.25.111.232/item_sets";

                foreach (string group in groups)
                {
                    var payload = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("record[title]", group),
                new KeyValuePair<string, string>("record[group_name]", "Staff Groups"),
                new KeyValuePair<string, string>("record[is_rentable]", "0"),
                new KeyValuePair<string, string>("image_group_select", "custom"),
                new KeyValuePair<string, string>("commit", "Create"),
            };

                    var content = new FormUrlEncodedContent(payload);
                    var post = await httpClient.PostAsync(url, content);
                    post.EnsureSuccessStatusCode();
                    GlobalLogger.Logger.Debug($"Group {group} uploaded successfully.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                GlobalLogger.Logger.Error($"HTTP error during group upload: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Unexpected error during group upload: {ex.Message}");
            }
        }

        public async Task<HttpResponseMessage> Search(ADUser user)
        {
            try
            {
                var searchCall = new Uri(url + $"/items?utf8=%E2%9C%93&search%5Bsystem_id%5D%5Bfrom%5D={user.Username}-AD");
                var response = await httpClient.GetAsync(searchCall);
                response.EnsureSuccessStatusCode();
                GlobalLogger.Logger.Debug($"Search successful for user {user.Username}");
                return response;
            }
            catch (HttpRequestException httpEx)
            {
                GlobalLogger.Logger.Error($"HTTP error during search: {httpEx.Message}");
                throw; // Re-throw to maintain return type
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Unexpected error during search: {ex.Message}");
                throw; // Re-throw to maintain return type
            }
        }

        public void Dispose()
        {
            try
            {
                GC.SuppressFinalize(this);
                GlobalLogger.Logger.Debug("Resources disposed.");
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"Error during disposal: {ex.Message}");
            }
        }

    }
}