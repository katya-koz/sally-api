using SALLY_API.Entities;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Data;
using SALLY_API.Entities;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace SALLY_API
{
    internal class Configuration
    {
        protected string? IP = Environment.GetEnvironmentVariable("ldapIP");
        protected string? USER = Environment.GetEnvironmentVariable("ldapUser");
        protected string? PASS = Environment.GetEnvironmentVariable("ldapPass");
    }

    internal class ADService : Configuration, IDisposable
    {
        private ADUser GetADUser(UserPrincipal user)
        {


            try
            {

                
                //get the user information 
                DateTime? ExpirationDate = user.AccountExpirationDate;
                DirectoryEntry de = user.GetUnderlyingObject() as DirectoryEntry;

                string department = de.Properties["department"].Value as string ?? "missing";
                string role = de.Properties["Title"].Value as string ?? "missing";
                string manager = CleanManager(de.Properties["manager"].Value as string ?? "missing");
                string Attribute = de.Properties["extensionAttribute13"].Value as string ?? "missing";
                string FirstName = user.GivenName ?? "missing";
                string LastName = user.Surname ?? "missing";
                string employeenumber = de.Properties["employeeNumber"].Value as string ?? "missing";
                string email = user.EmailAddress ?? "missing";
                string userName = user.SamAccountName ?? "missing";
                string badge = "missing";
               
               
              


                //store in the user info class in Obj.cs
                ADUser userInfo = new ADUser
                {
                    Username = userName,
                    Name = user.Name ?? "missing",
                    Role = role,
                    Department = department,
                    ExpirationDate = ExpirationDate,
                    Manager = manager,
                    BadgeID = badge,
                    Firstname = FirstName,
                    Lastname = LastName,
                    EmployeeNumber = employeenumber,
                    Email = email,
                };




                return userInfo;
            }
            catch (Exception ex)
            {

                return null;
            }
        }
        private static string CleanManager(string manager)
        {
            if (manager.Equals("missing"))
            {
                return manager;
            }

            else
            {
                return manager.Split(',')[0].Split('=')[1];

            }
        }




        public ADService() { }

        public void GetUsersAll()
        {
            List<ADUser> userInfoList = new List<ADUser>();

            using (SQL db = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE_STAGING")))
            {
                db.TruncateTableAD().Wait();
                using (var context = new PrincipalContext(ContextType.Domain, IP, USER, PASS))
                {
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                    {
                        foreach (var entry in searcher.FindAll())
                        {
                            UserPrincipal user = entry as UserPrincipal;
                            DirectoryEntry de = user.GetUnderlyingObject() as DirectoryEntry;
                            //get all users including inactive ones 
                            if (user != null)
                            {

                                // Retrieve user information and add to the list
                                ADUser userInfo = GetADUser(user);
                                db.InsertAdUser(userInfo);

                            }
                        }
                    }
                }

            }
        }


        private bool IsActive(DirectoryEntry de)
        {
            if (de.NativeGuid == null) return false;
            int flags = (int)de.Properties["userAccountControl"].Value;
            return !Convert.ToBoolean(flags & 0x0002);


            //     return true; 
        }

        public async Task<List<ADUser>> ADUsersSearchAsyncWithIdeals(string AD)
        {
            List<ADUser> userListwithIdeals = new List<ADUser>();
            List<ADUser> userListwithoutideals = await ADUsersSearchAsync(AD);
            try
            {
                using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE")))
                {
                    foreach (ADUser user in userListwithoutideals)
                    {
                        userListwithIdeals.Add(sql.GetIdeals(user));



                    }
                }

                GlobalLogger.Logger.Debug("Looking for user badges!");
                // get the users' badges from activate
                // get old badge from activate (not important for upsert, but needed for logging) 
                using (SQL sql = new SQL(Server.Activate, Environment.GetEnvironmentVariable("Activate_DB")))
                {
                    foreach (ADUser user in userListwithIdeals)
                    {
                        if (user.Username != null && user.Username != "" && user.Username != "missing")
                        {
                            GlobalLogger.Logger.Debug(user.Username);
                            user.BadgeID = sql.GetCurrentActivateBadgeByUsername(user.Username);

                        };
                    }
                };
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error(ex.ToString());
            }
            return userListwithIdeals;
        }


        public async Task<List<ADUser>> ADUserSearchAsyncWithIdeals(string AD)
        {

            List<ADUser> userListwithIdeals = new List<ADUser>();
            List<ADUser> userListwithoutideals = await ADUserSearchAsync(AD);
            try
            {
                using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE")))
                {
                    foreach (ADUser user in userListwithoutideals)
                    {
                        userListwithIdeals.Add(sql.GetIdeals(user));



                    }
                }

                

            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Error(ex.ToString());
            }
            return userListwithIdeals;
        }





        public async Task<List<ADUser>> ADUserSearchAsync(string AD)
        {
            List<ADUser> userList = new List<ADUser>();

            using (var context = new PrincipalContext(ContextType.Domain, IP, USER, PASS))
            {
                // create a userprincipal object with the search criteria
                UserPrincipal userFilter = new UserPrincipal(context);

                userFilter.SamAccountName = AD;


                // ccreate a principalsearcher with the user filter
                using (var searcher = new PrincipalSearcher(userFilter))
                {
                    PrincipalSearchResult<Principal> result = await Task.Run(() => searcher.FindAll());

                    // find the first matching user
                    foreach (UserPrincipal user in result)
                    {


                        DirectoryEntry de = user.GetUnderlyingObject() as DirectoryEntry;

                        if (user != null && IsActive(de))
                        {
                            
                            ADUser userInfo = GetADUser(user);
                            userList.Add(userInfo);

                        }
                        else
                        {
                            if (user == null)
                            {
                                GlobalLogger.Logger.Debug("null");

                            }
                            else
                            {
                                GlobalLogger.Logger.Debug("account isn't active");

                            }

                        }

                    }

                    return userList;

                }
            }

        }


        private async Task<List<ADUser>> ADUsersSearchAsync(string AD)
        {
            List<ADUser> userList = new List<ADUser>();

            using (var context = new PrincipalContext(ContextType.Domain, IP, USER, PASS))
            {
               
                UserPrincipal userFilter = new UserPrincipal(context);

                userFilter.SamAccountName = "*" + AD + "*";


                using (var searcher = new PrincipalSearcher(userFilter))
                {
                    PrincipalSearchResult<Principal> result = await Task.Run(() => searcher.FindAll());

                    foreach (UserPrincipal user in result)
                    {



                        DirectoryEntry de = user.GetUnderlyingObject() as DirectoryEntry;

                        if (user != null && IsActive(de))
                        {
                            ADUser userInfo = GetADUser(user);
                            userList.Add(userInfo);

                        }

                    }

                    return userList;

                }
            }

        }


        public async Task<string> Authentication(string username, string password, string application)

        {

            string json = "";


            using (var context = new PrincipalContext(ContextType.Domain, IP, USER, PASS))

            {

                bool IsValid = context.ValidateCredentials(username, password);

                if (IsValid)

                {

                    UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                    DirectoryEntry de = user.GetUnderlyingObject() as DirectoryEntry;

                    string userManager = CleanManager(de.Properties["manager"].Value as string ?? "missing");

                    string userDepartment = de.Properties["department"].Value as string ?? "missing";

                    string userRole = de.Properties["Title"].Value as string ?? "missing";

                    string userEmail = user.EmailAddress ?? "missing";

                    List<string> roles = new List<string>();
                    foreach (Principal group in user.GetGroups())
                    {

                        roles.Add(group.ToString().ToLower());
                    }

                    DataTable results = new DataTable();

                    using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE")))

                    {

                        results = sql.GetSecurityRulesForApplication(application);

                    }

                    // a user may have multiple security roles

                    List<string> securityRoles = new List<string>();


                    foreach (DataRow row in results.Rows)

                    {
                       


                        string department = row.Field<string>("Department");

                        string role = row.Field<string>("Role");

                        string manager = row.Field<string>("Manager");

                        string adUser = row.Field<string>("ADUser");

                        string security = row.Field<string>("SecurityRole");

                        string group = row.Field<string>("Group");

                      


                        if ((string.IsNullOrEmpty(department) || department.ToLower() == userDepartment.ToLower()) &&

                            (string.IsNullOrEmpty(role) || role.ToLower() == userRole.ToLower()) &&

                            (string.IsNullOrEmpty(manager) || manager.ToLower() == userManager.ToLower()) &&

                            (string.IsNullOrEmpty(adUser) || adUser.ToLower() == username.ToLower()) &&

                            string.IsNullOrEmpty(group) || roles.Contains(group.ToLower()))

                        {
                            
                            securityRoles.Add(security);

                        }

                    }

                    string rolesJson = JsonConvert.SerializeObject(securityRoles);


                    json = $"{{\"Name\":\"{user.GivenName} {user.Surname}\", \"ADUser\":\"{user.SamAccountName}\", \"Roles\":{rolesJson}}}";
       
                }

            }

            return json;

        }



        public void Dispose() { }
    }
}