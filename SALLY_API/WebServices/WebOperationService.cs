using Microsoft.AspNetCore.Mvc;
using SALLY_API.Entities;

namespace SALLY_API.WebServices
{

    public class UpsertResult
    {
        public DateTime ? DateTime { get; set; }
        public string ? User { get; set; }
        public bool UnassignBadgeSuccess { get; set; }
        public bool HHUserOperationSuccess { get; set; }
        public string HHUserOperation { get; set; }
        public bool ActivateUserOperationSuccess { get; set; }
        public string ActivateUserOperation { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return this.DateTime.ToString()+","+this.User;
        }
    }
    internal class WebOperationService : IDisposable
    {
        private HHWebOperations _hhWebOperations = new HHWebOperations();
        private ActivateWebOperations _activateWebOperations = new ActivateWebOperations();

        public static async Task<WebOperationService> CreateWebOperationServiceAsync() // factory to do async setup
        {
            var service = new WebOperationService();
            await service.LogInToApps();
        
            return service;
        }
        private async Task LogInToApps()
        {
            await _hhWebOperations.Login();
            await _activateWebOperations.Login();
        }
        internal async Task DeleteUser(ADUser user)
        {
            if (user.ActivateUser.ItemID != null && user.ActivateUser.ItemID > 0)
            {
                await _activateWebOperations.Delete(user);
                //deletedActivateUsers++;
            }
            else if (user.HHUser.ItemID != null && user.HHUser.ItemID > 0)
            {
                await _hhWebOperations.Archive(user);
                await _hhWebOperations.Delete(user);
               // deletedHHUsers++;
            }

        }
        internal async Task<UpsertResult> UpsertUser(ADUser user)
        {
            bool unassignBadgeSuccess = false;
            bool hhUserOperationSuccess = false;
            bool activateUserOperationSuccess = false;
            string hhUserOperation = ""; // "Update" or "Create"
            string activateUserOperation = ""; // "Update" or "Create"

            GlobalLogger.Logger.Debug("&&&&&&&&&&&&&&&&&&&&&&&&_Upsert User Task_&&&&&&&&&&&&&&&&&&&&&&&&");

            GlobalLogger.Logger.Debug("Upserting user: \n" + user.ToString());

            try
            {
                
                unassignBadgeSuccess = await UnassignBadge(user);

                GlobalLogger.Logger.Debug("Beggining HH user update/create...");

                {
                   
                    if (user.HHUser.ItemID != null)
                    {
                        GlobalLogger.Logger.Debug("Updating HHUser.");
                        hhUserOperation = "update";
                        try
                        {
                            var result = await _hhWebOperations.Update(user);
                            if (result == UpsertStatus.Success)
                            {
                                hhUserOperationSuccess = true;
                            }


                        }
                        catch
                        {
                            hhUserOperationSuccess = false;
                        }
                    }
                    else
                    {
                        hhUserOperation = "creation";
                        try
                        {
                            GlobalLogger.Logger.Debug("Creating HHUser.");
                            var result = await _hhWebOperations.Create(user);
                            if (result == UpsertStatus.Success)
                            {
                                hhUserOperationSuccess = true;
                            }

                        }
                        catch
                        {
                            hhUserOperationSuccess = false;
                        }
                    }

                    // Upsert in ActivateWebOperations
                    GlobalLogger.Logger.Debug("Beggining Activate user update/create...");
                    if (user.ActivateUser.ItemID != null)
                    {
                        activateUserOperation = "update";
                        try
                        {
                            GlobalLogger.Logger.Debug("Updating Activate User.");
                            var result = await _activateWebOperations.Update(user);

                            if (result == UpsertStatus.Success)
                            {
                                activateUserOperationSuccess = true;
                            }

                        }
                        catch
                        {
                            activateUserOperationSuccess = false;
                        }
                    }
                    else
                    {
                        activateUserOperation = "creation";
                        try
                        {
                            GlobalLogger.Logger.Debug("Creating Activate User.");
                            var result = await _activateWebOperations.Create(user);
                            if (result == UpsertStatus.Success)
                            {
                                activateUserOperationSuccess = true;
                            }
                        }
                        catch
                        {
                            activateUserOperationSuccess = false;
                        }
                    }

                    // Construct the message
                    string message = $"Hand Hygiene profile {hhUserOperation}: {(hhUserOperationSuccess ? "Success" : "Failed")}.\n " +
                                     $"Activate profile  {activateUserOperation}: {(activateUserOperationSuccess ? "Success" : "Failed")}.";

                    GlobalLogger.Logger.Debug(message);

                    // Return the result
                    return new UpsertResult
                    {   User = user.Username,
                        DateTime = DateTime.Now,
                        UnassignBadgeSuccess = unassignBadgeSuccess,
                        HHUserOperationSuccess = hhUserOperationSuccess,
                        HHUserOperation = hhUserOperation,
                        ActivateUserOperationSuccess = activateUserOperationSuccess,
                        ActivateUserOperation = activateUserOperation,
                        Message = message
                    };
                }
            }

            catch (Exception ex)
            {
                GlobalLogger.Logger.Debug($"General failure in UpsertUser: {ex}");
                return new UpsertResult
                {
                    UnassignBadgeSuccess = false,
                    HHUserOperationSuccess = false,
                    ActivateUserOperationSuccess = false,
                    Message = "General failure in UpsertUser."
                };
            }

        }

        //internal async Task CleanHH(List<ADUser> creates)
        //{

        //    using (HHWebOperations hHWebOperations = new HHWebOperations())
        //    {
        //        // Take the top 100 users
        //        var top100Creates = creates.Take(100).ToList();

        //        // First attempt
        //        List<ADUser> firstFailures = await hHWebOperations.BulkCreate(top100Creates);
        //        var firstSuccesses = top100Creates.Except(firstFailures);

        //        Console.WriteLine("Successfully Created ADUsers (First Attempt): " + firstSuccesses.Count());
        //        foreach (var user in firstSuccesses)
        //        {
        //            Console.WriteLine($"Name: {user.Name}");
        //        }

        //        // Print the names and badge numbers of failed ADUsers from the first attempt
        //        Console.WriteLine("\nFailed ADUsers (First Attempt): " + firstFailures.Count);
        //        var firstRoleCounts = firstFailures
        //            .GroupBy(user => user.Role)
        //            .Select(group => new { Role = group.Key, Count = group.Count() })
        //            .ToList();


        //        // Print the counts for each unique GroupKeys
        //        foreach (var roleCount in firstRoleCounts)
        //        {
        //            Console.WriteLine($"GroupKeys:{roleCount.Role}, Failures: {roleCount.Count}");
        //        }

        //        // Second attempt
        //        List<ADUser> secondFailures = await hHWebOperations.BulkCreate(top100Creates);
        //        var secondSuccesses = top100Creates.Except(secondFailures);

        //        Console.WriteLine("\nSuccessfully Created ADUsers (Second Attempt): " + secondSuccesses.Count());
        //        foreach (var user in secondSuccesses)
        //        {
        //            Console.WriteLine($"Name: {user.Name}");
        //        }

        //        // Print the names and badge numbers of failed ADUsers from the second attempt
        //        Console.WriteLine("\nFailed ADUsers (Second Attempt): " + secondFailures.Count);
        //        var SecondRoleCount = firstFailures
        //            .GroupBy(user => user.Role)
        //            .Select(group => new { Role = group.Key, Count = group.Count() })
        //            .ToList();


        //        // Print the counts for each unique GroupKeys
        //        foreach (var roleCount in SecondRoleCount)
        //        {
        //            Console.WriteLine($"GroupKeys:{roleCount.Role}, Failures: {roleCount.Count}");
        //        }

        //        // Check if the same users failed in both attempts
        //        var repeatedFailures = firstFailures.Intersect(secondFailures).ToList();
        //        Console.WriteLine("\nRepeated Failures: " + repeatedFailures.Count);
        //        foreach (var user in repeatedFailures)
        //        {
        //            Console.WriteLine($"Name: {user.Name}, Badge Number: {user.BadgeID}, Role: {user.Role}");
        //        }


        //    }

        //}
            //Security Interface will need to use this function but probably would need to confirm that the badge is correct before is invoking it 
            private async Task<bool> UnassignBadge(ADUser aduser, int system=0)
        {
            GlobalLogger.Logger.Debug("Beggining steps to unassign conflicting badges with user...");
            if (aduser != null)
            {
                //HHWebOperations hHWebOperations = new HHWebOperations();
                //ActivateWebOperations activateWebOperations = new ActivateWebOperations();
                ADUser HHUser = new ADUser();
                ADUser ActivateUser = new ADUser();
                if (system==0 || system == 1)
                {

                    using (SQL sql = new SQL(Server.Activate, Environment.GetEnvironmentVariable("Activate_DB")))
                    {

                        ActivateUser = sql.SearchUserFromActivate(aduser.BadgeID);

                    }

                }
                if (system == 0 || system == 2)
                {

                    using (SQL sql = new SQL(Server.HH, Environment.GetEnvironmentVariable("HH_DB")))
                    {
                        HHUser = sql.SearchUserFromHH(aduser.BadgeID);
                    }

                }


                try
                {
                    if (HHUser.HHUser.ItemID != 0 && HHUser.HHUser.ItemID!=null &&!aduser.Username.ToLower().Equals(HHUser.Username.ToLower()))
                            {
                        GlobalLogger.Logger.Debug("Archiving then deleting Hand Hygiene user: \n"+HHUser.ToString());

                        await _hhWebOperations.Archive(HHUser);
                        await _hhWebOperations.Delete(HHUser); // added to delete user. need to archive user first

                            }
                    if (ActivateUser.ActivateUser.ItemID != 0 && ActivateUser.ActivateUser.ItemID != null && !aduser.Username.ToLower().Equals(ActivateUser.Username.ToLower()))
                            {
                            GlobalLogger.Logger.Debug("Deleting Activate user: \n" + ActivateUser.ToString());
                            await _activateWebOperations.Delete(ActivateUser);
                            }
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        //public async Task CreateMissingUsers()
        //{
        //    List<ADUser> UserstoCreate = new List<ADUser>();
        //    GlobalLogger.Logger.Debug("CreateMissingUsers function start");
        //    using (SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE")))
        //    {
        //        UserstoCreate = sql.GetCreateUsersList();

        //    }


        //    GlobalLogger.Logger.Debug("User List first entry: " + UserstoCreate.First());
        //    ActivateWebOperations ActivateOps = new ActivateWebOperations();
        //    HHWebOperations HHOps = new HHWebOperations();
        //    await ActivateOps.Login();
        //    GlobalLogger.Logger.Debug("creating users");

        //    foreach (ADUser user in UserstoCreate)
        //    {

        //        if (user.HHUser.RoleKey != null)
        //        {
        //            try
        //            {
        //                ActivateOps.Create(user);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.Write("failed to create Activate User " + ex.ToString());
        //            }
        //        }
        //        if (user.ActivateUser.DepartmentKey != null)
        //        {
        //            try
        //            {
        //                HHOps.Create(user);

        //            }
        //            catch (Exception ex)
        //            {
        //                GlobalLogger.Logger.Debug("failed to create HH User " + ex.ToString());
        //            }
        //        }

        //    }
        //}
        /*
         * Create a single AD User for Security Interface
         */
        //private async Task CreateUser(ADUser user)
        //{
        //    ActivateWebOperations ActivateOps = new ActivateWebOperations();
        //    HHWebOperations HHOps = new HHWebOperations();
        //    try
        //    {
        //        ActivateOps.Create(user);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Write("failed to create Activate User " + ex.ToString());
        //    }

        //    try
        //    {
        //        // await HHOps.Login(Environment.GetEnvironmentVariable("HH_USERNAME"), Environment.GetEnvironmentVariable("HH_PASSWORD"));
        //        //     HHOps.Create(user);

        //    }
        //    catch (Exception ex)
        //    {
        //        GlobalLogger.Logger.Debug("failed to create HH User " + ex.ToString());
        //    }

        //}

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
