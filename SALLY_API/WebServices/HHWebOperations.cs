using SALLY_API.Entities;
using SALLY_API.Interfaces;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;





namespace SALLY_API.WebServices
{
    internal class HHWebOperations : IWebOperations, IDisposable
    {
        private string username { get; set; }

        private string password { get; set; }


        private CookieContainer cookiebox { get; set; }
        private HttpClientHandler handler { get; set; }

        private static UriBuilder url = new UriBuilder((Environment.GetEnvironmentVariable("HH_ROOT_URL")));
        private HttpClient httpClient { get; set; }


        // delegates
        public delegate Task HTTPRequestModifier(List<KeyValuePair<string, string>> payload, ADUser user);

        public HHWebOperations()
        {
            try
            {
                username = Environment.GetEnvironmentVariable("HH_USERNAME");
                password = Environment.GetEnvironmentVariable("HH_PASSWORD");
                cookiebox = new CookieContainer();

                handler = new HttpClientHandler()
                {
                    CookieContainer = cookiebox,
                };
                httpClient = new HttpClient(handler);
                httpClient.BaseAddress = url.Uri;
                httpClient.DefaultRequestHeaders.ExpectContinue = false;
                httpClient.DefaultRequestHeaders.ConnectionClose = true;
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
                httpClient.DefaultRequestHeaders.Referrer = new Uri($"{httpClient.BaseAddress}login");
                httpClient.DefaultRequestHeaders.Add("Origin", httpClient.BaseAddress.ToString());
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error initializing HHWebOperations: {e.Message}");
            }
        }

        public async Task<UpsertStatus> PerformUserProfileAction(ADUser user, HTTPRequestModifier requestModifier)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync("/staff_members/new?adapter=_list_inline_adapter");
                response.EnsureSuccessStatusCode();

                //string html = await response.Content.ReadAsStringAsync();
                //HtmlDocument doc = new HtmlDocument();
                //doc.LoadHtml(html);

                var payload = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("record[first_name]", user.Firstname),
            new KeyValuePair<string, string>("record[last_name]", user.Lastname),
            new KeyValuePair<string, string>("record[external_staff_identifier]", user.Username),
            new KeyValuePair<string, string>("record[email]", user.Email),
            new KeyValuePair<string, string>("record[badge]", user.BadgeID),
            new KeyValuePair<string, string>("record[role]", ""+user.HHUser.RoleKey),
            new KeyValuePair<string, string>("record[groups][]", ""),
        };

                foreach (int group in user.HHUser.GroupKeys)
                {
                    payload.Add(new KeyValuePair<string, string>("record[groups][]", "" + group));
                }

                await requestModifier(payload, user);
                var validation = await ValidateAction(user);
                return validation;
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error performing user profile action: {e.Message}");
                return UpsertStatus.HHUpdateFailed;
            }
        }

        private async Task HTTPUpdateModifier(List<KeyValuePair<string, string>> payload, ADUser user)
        {

            GlobalLogger.Logger.Debug("************HAND HYGIENE UPDATE***************");
            GlobalLogger.Logger.Debug(user.ToString());
            try
            {
                payload.Add(new KeyValuePair<string, string>("commit", "Update"));
                payload.Add(new KeyValuePair<string, string>("_method", "patch"));

                FormUrlEncodedContent content = new FormUrlEncodedContent(payload);

                HttpResponseMessage post = await httpClient.PostAsync($"/staff_members/{user.HHUser.ItemID}", content);
                post.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error updating user: {e.Message}");
                throw;
            }
        }

        private async Task HTTPCreateModifier(List<KeyValuePair<string, string>> payload, ADUser user)
        {
            GlobalLogger.Logger.Debug("************HAND HYGIENE CREATE***************");
            GlobalLogger.Logger.Debug(user.ToString());
            try
            {
                payload.Add(new KeyValuePair<string, string>("commit", "Create"));

                FormUrlEncodedContent content = new FormUrlEncodedContent(payload);
                HttpResponseMessage post = await httpClient.PostAsync("/staff_members", content);
                //string htmlResponse = await post.Content.ReadAsStringAsync();
               // GlobalLogger.Logger.Debug(htmlResponse);
                post.EnsureSuccessStatusCode();
                GlobalLogger.Logger.Debug("HHUser created successfully.");
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error creating user: {e.Message}");
                throw;
            }
        }

        public async Task<UpsertStatus> Create(ADUser user)
        {
            try
            {
                //await Login();// allegedly, we should already be logged in!

                var result = await PerformUserProfileAction(user, HTTPCreateModifier);

                return result;
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error in Create method: {e.Message}");
                throw new Exception($"Error in Create method: {e.Message}");
            }
        }
        //public async Task<List<ADUser>> BulkCreate(List<ADUser> users)
        //{
        //    await Login();
        //    List<ADUser> failcount = new List<ADUser>();
        //    int count = 0; 
        //    foreach (ADUser user in users) {
        //        try
        //        {
        //            using (SQL sql = new SQL(Server.HH, Environment.GetEnvironmentVariable("HH_DB")))
        //            {
        //                ADUser archive= sql.SearchUserFromHH(user.BadgeID);
        //                if (archive.HHUser.ItemID != null) {

        //                    await Archive(archive);

        //                }
        //            }
                    

        //            await PerformUserProfileAction(user, HTTPCreateModifier);
        //            count++;
        //            UpsertStatus status=await ValidateAction(user);
        //            if (status.HasFlag(UpsertStatus.HHUpdateFailed))
        //            {
        //                failcount.Add(user);
        //            }
        //        }
        //        catch (Exception e) {
        //            failcount.Add(user);
        //        GlobalLogger.Logger.Error($"Error in create method: { e.Message}");
        //        }
        //        if (count == 99)
        //        {
        //            return failcount;
        //        }


        //    }
        //    return failcount; 
        //}

        public async Task<UpsertStatus> Update(ADUser user)
        {
            try

            {
                var result = await PerformUserProfileAction(user, HTTPUpdateModifier);
                return result;
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error in Update method: {e.Message}");
                throw new Exception($"Error in Update method: {e.Message}");
            }
        }

        public async Task Delete(ADUser user)
        {

            try
            {
                var payload = new List<KeyValuePair<string, string>>
        {
                new KeyValuePair<string, string>("commit","Delete"),
                new KeyValuePair<string, string>("_method", "delete"),
                new KeyValuePair<string, string>("utf8","✓")
        };
                var content = new FormUrlEncodedContent(payload);

                var response = await httpClient.PostAsync($"/staff_members/{user.HHUser.ItemID}?archived=true", content);
                response.EnsureSuccessStatusCode();
                GlobalLogger.Logger.Debug($"Hand Hygiene delete request sent for user {user.Username}");
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error in Delete method: {e.Message}");
            }
        }

        public async Task Archive(ADUser user)
        {
            try
            {
                var payload = new List<KeyValuePair<string, string>>
        {
                new KeyValuePair<string, string>("commit","Archive"),
                new KeyValuePair<string, string>("_method", "patch"),
        };
                var content = new FormUrlEncodedContent(payload);
                var response = await httpClient.PostAsync($"/staff_members/{user.HHUser.ItemID}/archive", content);
                response.EnsureSuccessStatusCode();
                GlobalLogger.Logger.Debug($"Hand Hygiene archive request sent for user {user.Username}");
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error in Archive method: {e.Message}");
            }
        }

        public async Task Login()
        {
            try
            {
                var response = await httpClient.GetAsync("/login");
                response.EnsureSuccessStatusCode();

                string authenticity_token = await GetToken(response);

                var keyValuePairs = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("utf8", "✓"),
            new KeyValuePair<string,string>("authenticity_token",authenticity_token),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("commit", "Login")
        };

                FormUrlEncodedContent content = new FormUrlEncodedContent(keyValuePairs);
                response = await httpClient.PostAsync("/user_sessions", content);

                if (!response.IsSuccessStatusCode)
                
                {
                    GlobalLogger.Logger.Error($"Login failed: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error during login: {e.Message}");
                throw;
            }
        }


        /* cases:
            Search
                - user not found

            Delete/Archive
                - user not found in archive

            Create
                - need to prevent/add a check to make sure staff-identifiers are not being repeated
                - user not found (check if user exists by staff-identifier, and if they have the same properties as user beign searched for)

            Update
                - user not found (check if user exists by staff-identifier, and if they have the same properties as user beign searched for) - update didnt go through, check if same badge number exists?


        */
        public async Task<HttpResponseMessage> Search(ADUser user)
        {
            try
            {
                Uri searchcall = new Uri(url + "staff_members?utf8=%E2%9C%93&search=" + user.Username + "&commit=Search");
                var response = await httpClient.GetAsync(searchcall);
                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync();
                return response;
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error in Search method: {e.Message}");
                throw;
            }
        }

        private void LogValidation(ADUser ideal, ADUser actual)
        {
            int[] idealGroupKeysSorted = ideal.HHUser.GroupKeys.OrderBy(n => n).ToArray();
            int[] actualGroupKeysSorted = actual.HHUser.GroupKeys.OrderBy(n => n).ToArray();
            int max = Math.Max(idealGroupKeysSorted.Length, actualGroupKeysSorted.Length);

            var sb = new StringBuilder();
            sb.AppendLine("Validation Results:");
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "Field", "Ideal", "Actual"));
            sb.AppendLine(new string('-', 80));
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "Name:", $"{ideal.Firstname} {ideal.Lastname}", $"{actual.Firstname} {actual.Lastname}"));
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "Username:", ideal.Username, actual.Username));
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "ItemId:", ideal.HHUser.ItemID, actual.HHUser.ItemID));
            sb.AppendLine(string.Format("{0,-20} {1,-60} {2,-60}", "RoleKey:", ideal.HHUser.RoleKey, actual.HHUser.RoleKey));
            sb.AppendLine(string.Format("{0,-20}", "GroupKeys:"));


            for (int i = 0; i < max; i++)
            {
                string idealKey = i < idealGroupKeysSorted.Length ? idealGroupKeysSorted[i].ToString() : "";
                string actualKey = i < actualGroupKeysSorted.Length ? actualGroupKeysSorted[i].ToString() : "";

                sb.AppendLine(string.Format("{0,-20} {1,-60}", "", $"{idealKey}", $"{actualKey}"));
            }

            GlobalLogger.Logger.Debug(sb.ToString());
        }

        public async Task UploadRoles(List<string> roles)
        {
            try
            {
                await Login();
                string url = "http://172.25.111.234/roles";

                foreach (string role in roles)
                {
                    try
                    {
                        var payload = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("record[name]", role),
                    new KeyValuePair<string, string>("commit", "Create"),
                };

                        FormUrlEncodedContent content = new FormUrlEncodedContent(payload);
                        HttpResponseMessage post = await httpClient.PostAsync(url, content);
                        post.EnsureSuccessStatusCode();

                        GlobalLogger.Logger.Debug($"Role '{role}' uploaded successfully.");
                    }
                    catch (Exception innerEx)
                    {
                        GlobalLogger.Logger.Debug($"Error uploading role '{role}': {innerEx.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Debug($"Error in UploadRoles method: {e.Message}");
            }
        }
        public async Task UploadGroups(List<string> groups)
        {
            try
            {
                await Login();
                string url = "http://172.25.111.234/groups";

                foreach (string group in groups)
                {
                    try
                    {
                        var payload = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("record[name]", group),
                    new KeyValuePair<string, string>("record[compliance_goal]", "100"),
                    new KeyValuePair<string, string>("commit", "Create"),
                };

                        FormUrlEncodedContent content = new FormUrlEncodedContent(payload);
                        HttpResponseMessage post = await httpClient.PostAsync(url, content);
                        post.EnsureSuccessStatusCode();

                        GlobalLogger.Logger.Debug($"Group '{group}' uploaded successfully.");
                    }
                    catch (Exception innerEx)
                    {
                        GlobalLogger.Logger.Error($"Error uploading group '{group}': {innerEx.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error in UploadGroups method: {e.Message}");
            }
        }
        private async Task<UpsertStatus> ValidateAction(ADUser ideal)
        {
            try
            {

                ADUser actual = new ADUser();
                actual.HHUser = new HHUser(-1, -1, new List<int>());
                using (SQL sql = new SQL(Server.HH, Environment.GetEnvironmentVariable("HH_DB")))
                {
                    actual = sql.SearchUserFromHH(ideal.BadgeID);
                }

                GlobalLogger.Logger.Debug("Performing validation step for Activate user: ");

                LogValidation(ideal, actual);

                if (ideal.Username == actual.Username &&
                    ideal.HHUser.IsEqualTo(actual.HHUser))
                {
                    GlobalLogger.Logger.Debug("HH action validation successful for user: " + ideal.Username + " with badge: " + ideal.BadgeID);
                    return UpsertStatus.Success;
                }
                else
                {
                    GlobalLogger.Logger.Debug("HH action validation failed for user: " + ideal.Username + " with badge: " + ideal.BadgeID);
                    return UpsertStatus.HHUpdateFailed;
                }
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Debug($"HH action validation error: { e.Message}");
                return UpsertStatus.HHUpdateFailed;
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
                    GlobalLogger.Logger.Debug("CSRF token not found in GetToken.");
                    return ""; // Exception could be thrown here if necessary.
                }

                string token = match.Groups[1].Value;
                GlobalLogger.Logger.Debug($"CSRF token retrieved: {token}");
                return token;
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Error($"Error in GetToken method: {e.Message}");
                throw;
            }
        }
        public void Dispose()
        {
            try
            {
                GC.SuppressFinalize(this);
                GlobalLogger.Logger.Debug("Resources disposed successfully.");
            }
            catch (Exception e)
            {
                GlobalLogger.Logger.Debug($"Error in Dispose method: {e.Message}");
            }
        }

    }
}