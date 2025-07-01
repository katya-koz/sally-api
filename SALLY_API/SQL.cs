using System.Data;
using SALLY_API.Entities;
using Microsoft.Data.SqlClient;
using DocumentFormat.OpenXml.ExtendedProperties;
using SALLY_API.Entities.Handsify;
using System.Drawing.Drawing2D;
using SALLY_API.Controllers;
using static SALLY_API.Entities.Handsify.HHStation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;

///this class is getting too big needs to be split up 
// katya waz here
namespace SALLY_API
{
    internal enum Server
    {
        RTLS,
        HillRom,
        Vocera,
        BizTalkPoc1,
        BizTalkPoc2,
        HH,
        Activate
    }

    internal class SQL : IDisposable
    {
        private SqlConnection connection;

        internal SQL(Server server, string database)
        {
            string connectionString = GetConnectionString(server, database);

            try
            {
                connection = new SqlConnection(connectionString);
                connection.Open();
            }
            catch (SqlException ex)
            {
                throw new Exception("Could not connect to database: "+ex.Message, ex);
            }
        }
        ///database shouldn't be a string should be an enum or an environment variable 
        private string GetConnectionString(Server server, string database)
        {
            switch (server)
            {
                case Server.Vocera:
                    return ConnectionStringTemplate(Environment.GetEnvironmentVariable("VOCERA_SERVER"), database, Environment.GetEnvironmentVariable("SERVICE_USER"), Environment.GetEnvironmentVariable("SERVICE_PASS"));
                case Server.RTLS:
                    return ConnectionStringTemplate(Environment.GetEnvironmentVariable("RTLS_SERVER"), database, Environment.GetEnvironmentVariable("SERVICE_USER"), Environment.GetEnvironmentVariable("SERVICE_PASS"));
                case Server.HillRom:
                    return ConnectionStringTemplate(Environment.GetEnvironmentVariable("HILROM_SERVER"), database, Environment.GetEnvironmentVariable("SERVICE_USER"), Environment.GetEnvironmentVariable("SERVICE_PASS"));
                case Server.HH:
                    return ConnectionStringTemplate(Environment.GetEnvironmentVariable("CENTRAK_SERVER_HH"), database, Environment.GetEnvironmentVariable("CENTRAK_USER"), Environment.GetEnvironmentVariable("CENTRAK_PASS"));
                case Server.Activate:
                    return ConnectionStringTemplate(Environment.GetEnvironmentVariable("CENTRAK_SERVER_Activate"), database, Environment.GetEnvironmentVariable("CENTRAK_USER"), Environment.GetEnvironmentVariable("CENTRAK_PASS"));
                case Server.BizTalkPoc1:
                    return ConnectionStringTemplate(Environment.GetEnvironmentVariable("BIZTALKPOC1"), database, Environment.GetEnvironmentVariable("SERVICE_USER"), Environment.GetEnvironmentVariable("SERVICE_PASS"));
                default:
                    throw new ArgumentException("Server does not exist or has not been implemented yet.");
            }
        }


        private string ConnectionStringTemplate(string server, string database, string user, string pass)
        {
            return $"server={server}; database={database}; user id={user}; PASSWORD={pass}; TrustServerCertificate=True; MultipleActiveResultSets = True; ";
        }

        internal DataSet GetEMDataByDepartment(string department)
        {
            try
            {
                //fix this query put it in the ENV file or something 
                string query = $"SELECT * FROM [EM_TEST].[dbo].[EMReport] where Department = '{department}'";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                return ds;
            }
            catch (Exception ex)
            {
                GlobalLogger.Logger.Debug(ex.ToString());
                //Dispose();
                return new DataSet();
            }

        }
        internal async Task ReLoadDB()
        {
            string jobName= Environment.GetEnvironmentVariable("API_DB_RELOAD");
            try
            {
                using (SqlCommand command = new SqlCommand("palmerprotocol", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.ExecuteNonQuery();

                }
                using (SqlCommand command = new SqlCommand("msdb.dbo.sp_start_job", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@job_name", jobName);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine($"Failed to start job '{jobName}': {ex.Message}");
            }


        }
        //gets all ad users with their ideal roles and names 

        internal async Task<List<ADUser>> GetCleanUpADUsers()
        {
            string query = "select * from [CleanUpCreateOrUpdate]";
            SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
            Console.Write("filled adapter");
            List<ADUser> usersList = new List<ADUser>();

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    ADUser user = ParseADUser(row);

                    //load HH user the database will pass groups as CSV 
           
                    usersList.Add(user);

                }
            }
            return usersList;

        }


        public DataTable GetBadgeifyLogs()
        {

            DataTable dataTable = new DataTable();

            string query = Environment.GetEnvironmentVariable("GET_BADGEIFY_LOGS");
            SqlCommand command = new SqlCommand(query, connection);
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }


            return dataTable;
        }
        internal async Task<List<ADUser>> GetCleanUpDeleteADUsers()
        {
            string query = "select * from [CleanUpActivateHHDelete]";
            SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
            Console.Write("filled adapter");
            List<ADUser> usersList = new List<ADUser>();

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    ADUser user = ParseADUser(row);

                    usersList.Add(user);

                }
            }
            return usersList;

        }
        internal async Task<List<ADUser>> GetADUsers()
        {
            string query = "select * from GetADUsers";
            SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
            Console.Write("filled adapter");
            List<ADUser> usersList = new List<ADUser>();

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    ADUser user = ParseADUser(row);


                    usersList.Add(user);

                }
            }
            return usersList;
        }


        //generates 3 tables to be used as part of the firmware refresh 
        internal DataSet GetUKGReports()
        {
            // GlobalLogger.Logger.Debug("Got to stage 1");
            DataSet CombinedUKGReports = new DataSet();
            string query = Environment.GetEnvironmentVariable("UKG_REPORT_QUERY");

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.CommandTimeout = 300; // Set timeout in seconds
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                adapter.Fill(CombinedUKGReports);
            }
            CombinedUKGReports.Tables[0].TableName = "On Site Report";
            CombinedUKGReports.Tables[1].TableName = "Detailed Report";
            CombinedUKGReports.Tables[2].TableName = "Summary Report";
            GlobalLogger.Logger.Debug(CombinedUKGReports.Tables[0].Rows[0].ToString());
            GlobalLogger.Logger.Debug(CombinedUKGReports.Tables.Count.ToString());
            return CombinedUKGReports;
        }


        //returns a list of departments in the onsite report from UKG 
        private List<string> GetUKGDepartments()
        {
            DataSet ukgDepartments = new DataSet();
            string query = Environment.GetEnvironmentVariable("UKG_QUERY_Department");
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.CommandTimeout = 300; // Set timeout in seconds
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                adapter.Fill(ukgDepartments);


            }
            List<string> departmentList = ukgDepartments.Tables[0].AsEnumerable()
                              .Select(row => row[0].ToString())
                              .ToList();

            return departmentList;
        }

        private List<string> GetUKGDepartmentsTotal()
        {
            DataSet ukgDepartments = new DataSet();
            string query = Environment.GetEnvironmentVariable("UKG_QUERY_Department_Total");
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.CommandTimeout = 300; // Set timeout in seconds
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                adapter.Fill(ukgDepartments);


            }
            List<string> departmentList = ukgDepartments.Tables[0].AsEnumerable()
                              .Select(row => row[0].ToString())
                              .ToList();

            return departmentList;
        }
        internal DataSet GetUKGDepartmentReports()
        {
            DataSet CombinedUKGReports = new DataSet();
            List<string> departments = GetUKGDepartments();
            foreach (string department in departments)
            {
                string baseQuery = Environment.GetEnvironmentVariable("UKG_REPORT_QUERY_Department");
                string tableName = department.Replace(" ", "_");

                string query = $"{baseQuery} WHERE [Department Description] = '{department}' and datasource like '%cvh%';";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = 300; 
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    adapter.Fill(CombinedUKGReports, tableName);
                }
            }
            return CombinedUKGReports;
        }

        internal DataSet GetUKGDepartmentReportsTotal()
        {
            DataSet CombinedUKGReports = new DataSet();
            List<string> departments = GetUKGDepartmentsTotal();
            foreach (string department in departments)
            {
               
                string baseQuery = Environment.GetEnvironmentVariable("UKG_REPORT_QUERY_Department_Total");
                string tableName = department.Replace(" ", "_"); 

                string query = $"{baseQuery} WHERE [Department Description] = '{department}';";
             
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = 300;
                  
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    adapter.Fill(CombinedUKGReports, tableName); 
                }
            }

        
            return CombinedUKGReports;
        }
       
        internal string GetCurrentActivateBadgeByUsername(string username)
        {
            try
            {
                string query = $"SELECT TOP(1) tag_id FROM [CetaniRTLS].[dbo].[items] WHERE system_id like '%{username}-AD%' and discarded_at is null";
                


                using (var cmd = new SqlCommand(query, connection))
                {
                   
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        GlobalLogger.Logger.Debug("result: " + result);
                        return result.ToString();
                    }
                }
            }
            catch (SqlException e)
            {
                GlobalLogger.Logger.Error("Error getting current activate badge by user: " + e.ToString());
            }
            GlobalLogger.Logger.Debug("no badge found");
            return "missing";
        }

        //this class adds the ideal roles to an ADUser
        //its function is very simple it runs 3 sql queries and adds the values returned to the aduser object
        //while it does look large it doesn't actually have a high number of break points, the ussual issue would be in the sql queries and the col names
        internal ADUser GetIdeals(ADUser user)

        {
            try
            {
                string rolequery = $"select * from idealroledim where ADRole='{user.Role}'";
                string departmentquery = $"select * from idealdepartmentdim where ADDepartment='{user.Department}'";
                string departmentrolequery = Environment.GetEnvironmentVariable("HH_QUERY_ROLE_AND_DEPARTMENT").Replace("@ROLE", $"'{user.Role}'").Replace("@DEPARTMENT", $"'{user.Department}'");

                SqlDataAdapter roleadapter = new SqlDataAdapter(rolequery, connection);
                DataSet roleds = new DataSet();
                roleadapter.Fill(roleds);

                SqlDataAdapter departmentadapter = new SqlDataAdapter(departmentquery, connection);
                DataSet departmentds = new DataSet();
                departmentadapter.Fill(departmentds);

                SqlDataAdapter roledepartmentadapter = new SqlDataAdapter(departmentrolequery, connection);
                DataSet roledepartmentds = new DataSet();
                roledepartmentadapter.Fill(roledepartmentds);


                if (roleds.Tables.Count > 0 && roleds.Tables[0].Rows.Count > 0)
                {
                    if (roleds.Tables[0].Rows[0]["HHRole_Key"] != DBNull.Value &&
                        !string.IsNullOrWhiteSpace(roleds.Tables[0].Rows[0]["HHRole_Key"].ToString()))
                    {
                        user.HHUser.RoleKey = int.Parse(roleds.Tables[0].Rows[0]["HHRole_Key"].ToString());

                    }
                    else
                    {
                        user.HHUser.RoleKey = null; // Handle the case where the value is invalid.
                    }

                    user.ActivateUser.Role = roleds.Tables[0].Rows[0]["ActivateRole"].ToString();
                    foreach (DataRow row in roleds.Tables[0].Rows)
                    {
                        if (row["HHGroup_Key"] != DBNull.Value)
                        {
                            user.HHUser.GroupKeys.Add(int.Parse(row["HHGroup_Key"].ToString()));
                        }
                        if (row["ActivateGroup_Key"] != DBNull.Value)
                        {
                            user.ActivateUser.GroupKeys.Add(int.Parse(row["ActivateGroup_Key"].ToString()));
                        }
                    }

                }

                if (departmentds.Tables.Count > 0 && departmentds.Tables[0].Rows.Count > 0)
                {

                    if (departmentds.Tables[0].Rows[0]["ActivateDepartment_Key"] != DBNull.Value &&
                        !string.IsNullOrWhiteSpace(departmentds.Tables[0].Rows[0]["ActivateDepartment_Key"].ToString()))
                    {
                        user.ActivateUser.DepartmentKey = int.Parse(departmentds.Tables[0].Rows[0]["ActivateDepartment_Key"].ToString());
                    }
                    else
                    {
                        user.ActivateUser.DepartmentKey = null; 
                    }

                    foreach (DataRow row in departmentds.Tables[0].Rows)
                    {
                        if (row["HHDepartment_Key"] != DBNull.Value)
                        {
                            user.HHUser.GroupKeys.Add(int.Parse(row["HHDepartment_Key"].ToString()));
                        }
                        if (row["ActivateGroup_Key"] != DBNull.Value)
                        {
                            user.ActivateUser.GroupKeys.Add(int.Parse(row["ActivateGroup_Key"].ToString()));
                        }
                    }
                }
                if (roledepartmentds.Tables[0].Rows.Count > 0)
                {

                    user.HHUser.GroupKeys.Add(int.Parse(roledepartmentds.Tables[0].Rows[0]["roledepartmentkey"].ToString()));
                }

                


                return user;
            }
            catch (SqlException sqlEx)
            {
                // Log SQL errors
                GlobalLogger.Logger.Debug($"SQL Error: {sqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Log general errors
                GlobalLogger.Logger.Debug($"Error: {ex.Message}");
                throw;
            }

        }



        internal string? CheckBadge(string badgenumber)
        {

            string query = Environment.GetEnvironmentVariable("GET_UNASSIGNED_BADGES_ACTIVATE") + ("and tag_unique=" + badgenumber);
            SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
            string? name = "ERROR";
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {

                DataRow userrow = ds.Tables[0].Rows[0];
                name = userrow["Name"].ToString();
            }
            //using(SQL sql = new SQL(Server.HillRom, Environment.GetEnvironmentVariable("API_DATABASE_STAGING")))
            //{
            //   name= sql.CheckBadgePulse(badgenumber, name);
            //}
            return name;

        }

        internal void SaveStation(HHStation station, List<Note> newNotes, List<int> archivedNotes, int floor, string pod)
        {
            string procedure = "dbo.upsert_station";

            GlobalLogger.Logger.Debug("Starting SaveStation");
            GlobalLogger.Logger.Debug($"Stored Procedure: {procedure}");

            GlobalLogger.Logger.Debug("Station Parameters:");
            GlobalLogger.Logger.Debug($"  Name: {station.StationName}");
            GlobalLogger.Logger.Debug($"  X: {station.Coordinates.X}, Y: {station.Coordinates.Y}");
            GlobalLogger.Logger.Debug($"  Type: {station.StationType}");
            GlobalLogger.Logger.Debug($"  Location: {station.Location}");
            GlobalLogger.Logger.Debug($"  ID: {station.StationID}");
            GlobalLogger.Logger.Debug($"  Floor: {floor}");
            GlobalLogger.Logger.Debug($"  Pod: {pod}");
            GlobalLogger.Logger.Debug($"  StationKey: {station.StationKey}");
            GlobalLogger.Logger.Debug($"  Online: {station.OnlineStatus}");
            GlobalLogger.Logger.Debug($"  ModelResult: {station.ModelResult}");
            GlobalLogger.Logger.Debug($"  ArchivedNotes: {string.Join(",",archivedNotes)}");

         
            DataTable notesTable = new DataTable();
            notesTable.Columns.Add("Note_Key", typeof(long));
            notesTable.Columns.Add("NoteContent", typeof(string));
            notesTable.Columns.Add("Author", typeof(string));

            GlobalLogger.Logger.Debug("Adding new notes to DataTable:");
   
            foreach (var note in newNotes)
            {
                GlobalLogger.Logger.Debug($"  NoteKey: {note.NoteKey}, Author: {note.Author}, Content: {note.Content}");
                notesTable.Rows.Add(null, note.Content, note.Author); // Match order
            }

            using (SqlCommand cmd = new SqlCommand(procedure, connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@station_name", station.StationName);
                cmd.Parameters.AddWithValue("@xcoord", station.Coordinates.X);
                cmd.Parameters.AddWithValue("@ycoord", station.Coordinates.Y);
                cmd.Parameters.AddWithValue("@type", station.StationType);
                cmd.Parameters.AddWithValue("@id", station.StationID);
                cmd.Parameters.AddWithValue("@floor", floor);
                cmd.Parameters.AddWithValue("@pod", pod);
                cmd.Parameters.AddWithValue("@location", station.Location);
                cmd.Parameters.AddWithValue("@stationkey", station.StationKey);
                cmd.Parameters.AddWithValue("@onlinestatus", station.OnlineStatus);
                cmd.Parameters.AddWithValue("@modelresult", station.ModelResult);
                cmd.Parameters.AddWithValue("@archivedNotes", string.Join(",",archivedNotes));

                SqlParameter notesParam = new SqlParameter("@newNotes", SqlDbType.Structured)
                {
                    TypeName = "dbo.NoteTableType",
                    Value = notesTable
                };
                cmd.Parameters.Add(notesParam);

                GlobalLogger.Logger.Debug("SQL Parameters:");
                foreach (SqlParameter param in cmd.Parameters)
                {
                    string valueStr = param.Value is DataTable dt ? $"{dt.Rows.Count} rows in DataTable" : param.Value?.ToString();
                    GlobalLogger.Logger.Debug($"  {param.ParameterName} = {valueStr}");
                }

                cmd.ExecuteNonQuery();
                GlobalLogger.Logger.Debug("SaveStation completed successfully.");
            }
        }

    
        public string? CheckBadgePulse(string badgenumber, string name)
        {
            string pulsequery = Environment.GetEnvironmentVariable("PULSE_FIRMWARE_CHECK") + ("and [TAG ID]=" + badgenumber);
            SqlDataAdapter pulseadapter = new SqlDataAdapter(pulsequery, connection);
            DataSet pulseds = new DataSet();
            pulseadapter.Fill(pulseds);

            if (!(pulseds.Tables.Count > 0 && pulseds.Tables[0].Rows.Count > 0))
            {
                name = "ERROR";
            }
            return name;
        }

        internal List<int> GetBadges(int badgenumber)
        {
            string query;
            if (badgenumber < 0)
            {
                query = Environment.GetEnvironmentVariable("GET_UNASSIGNED_BADGES_ACTIVATE");
            }
            else
            {
                query = Environment.GetEnvironmentVariable("GET_UNASSIGNED_BADGES_ACTIVATE") + (" and link_date is null and tag_unique LIKE '%" + badgenumber + "%'");
            }
        
            SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
            List<int> badgesList = new List<int>();

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    badgesList.Add(int.Parse(row["Badge_Key"].ToString()));
                }
            }

            return badgesList;

        }

        internal void InsertBadgeifyUserLog(BadgeifyEvent e)
        {
            string insertBadgeifyUserLog = "dbo.InsertBadgeifyUserLog";
            using (SqlCommand command = new SqlCommand(insertBadgeifyUserLog, connection))
            {

                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = insertBadgeifyUserLog;
                command.Parameters.AddWithValue("@Actor", e.Actor);
                command.Parameters.AddWithValue("@TargetUser", e.Target.Username);
                command.Parameters.AddWithValue("@TargetUserName", $"{e.Target.Firstname} {e.Target.Lastname}");
                command.Parameters.AddWithValue("@TargetUserDepartment", e.Target.Department);
                command.Parameters.AddWithValue("@TargetUserEmail", e.Target.Email);
                command.Parameters.AddWithValue("@TargetUserRole", e.Target.Role);
                command.Parameters.AddWithValue("@TargetUserManager", e.Target.Manager);
                command.Parameters.AddWithValue("@TargetUserOldActivateBadge", e.OldBadge);
                command.Parameters.AddWithValue("@TargetUserNewBadgeAssignment", e.NewBadge);
                command.Parameters.AddWithValue("@HHAction", e.HHAction);
                command.Parameters.AddWithValue("@HHActionSuccess", e.HHActionSuccess);
                command.Parameters.AddWithValue("@ActivateAction", e.ActivateAction);
                command.Parameters.AddWithValue("@ActivateActionSuccess", e.ActivateActionSuccess);
                command.ExecuteNonQuery();
            }

        }
        private void UpsertADUser(ADUser user)
        {
            string UpsertADUser = "sally.UpsertADUser";
            using (SqlCommand command = new SqlCommand(UpsertADUser, connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = UpsertADUser;
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@name", user.Name);
                command.Parameters.AddWithValue("@role", user.Role);
                command.Parameters.AddWithValue("@Department", user.Department);
                command.Parameters.AddWithValue("@ExpirationDate", user.ExpirationDate);
                command.Parameters.AddWithValue("@manager", user.Manager);
                command.Parameters.AddWithValue("@badgeid", int.Parse(user.BadgeID));
                command.Parameters.AddWithValue("@email", user.Email);
                command.Parameters.AddWithValue("@firstname", user.Firstname);
                command.Parameters.AddWithValue("@lastname", user.Lastname);
                command.Parameters.AddWithValue("@employeenumber", user.EmployeeNumber);
                command.Parameters.AddWithValue("@hhitemid", user.HHUser.ItemID);
                command.Parameters.AddWithValue("@activateitemid", user.ActivateUser.ItemID);
                command.Parameters.AddWithValue("@activatedepartmentkey", user.ActivateUser.DepartmentKey);
                command.Parameters.AddWithValue("@activaterole", user.ActivateUser.Role);




                command.ExecuteNonQuery();

            }
        }

        internal async Task<HHStation> GetHHStation(int stationkey)
        {
            GlobalLogger.Logger.Debug("Get HH Station");

            string query = "[sally].GetStation";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Station_Key", stationkey);
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    return  await ParseHHStation(dt.Rows[0]); 
 




                }
            }

            }

        private async Task <HHStation> ParseHHStation(DataRow dataRow)
        {


            Coords coords = new Coords(int.Parse(dataRow["XCoord"].ToString()), int.Parse(dataRow["YCoord"].ToString()));

            Dictionary<int, Note> notes = await GetNotesForStation(dataRow["Station_Key"].ToString());

            HHStation station = new HHStation(
                dataRow["Name"].ToString(),
                dataRow["Location"].ToString(),
                dataRow["Type"].ToString(),
                Convert.ToBoolean(dataRow["status"]),
                int.Parse(dataRow["StationID"].ToString()),
                notes,
                coords,
                double.Parse(dataRow["ModelResult"].ToString()),
                int.Parse(dataRow["Station_Key"].ToString())
                );

            return station; 
        }
        internal DataTable GetSecurityRulesForApplication(string app)
        {
            string getSecurityRule = "sally.GetSecurityRule";

            using (SqlCommand command = new SqlCommand(getSecurityRule, connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Application", app);

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt); 

                    return dt; 
                }
            }
        }

        private int? GetSafeInt(DataRow row, string columnName)
        {
            var str = GetSafeString(row, columnName);
            return int.TryParse(str, out int result) ? result : (int?)null;
        }
        private void TryAddInt(List<int> list, int? value)
        {
            if (value.HasValue)
            {
                list.Add(value.Value);
            }
        }

        private ActivateUser ParseActivateUser(DataRow row)
        {
            ActivateUser ActivateUser = new ActivateUser();

            try
            {
                var activateUserKey = GetSafeInt(row, "ActivateUser_Key");
                if (activateUserKey.HasValue)
                {
                    ActivateUser.primarykey = activateUserKey.Value;
                }
                ActivateUser.DepartmentKey = GetSafeInt(row, "IdealActivateDepartment");
                ActivateUser.ItemID = GetSafeInt(row, "ActivateID");
                ActivateUser.Role = GetSafeString(row, "IdealActivateRole");

                TryAddInt(ActivateUser.GroupKeys, GetSafeInt(row, "idealActivateGrouprole"));
                TryAddInt(ActivateUser.GroupKeys, GetSafeInt(row, "IdealActivateGroupDep"));


            }

            catch (Exception ex)
            {
                GlobalLogger.Logger.Error($"An error occurred: {ex.Message}");


            }

            return ActivateUser;
        }
        private HHUser ParseHHUser(DataRow row)
        {
            HHUser HHuser = new HHUser();
            try
            {
                HHuser.primarykey = GetSafeInt(row, "HHUser_Key") ?? HHuser.primarykey;
                HHuser.ItemID = GetSafeInt(row, "HH_ID") ?? HHuser.ItemID;
                HHuser.RoleKey = GetSafeInt(row, "IDEALHHRole") ?? HHuser.RoleKey;

                var groupRoleKey = GetSafeInt(row, "IDEALHHGROUPROLE");
                if (groupRoleKey.HasValue) HHuser.GroupKeys.Add(groupRoleKey.Value);

                var departmentKey = GetSafeInt(row, "IDEALHHDEPARTMENT");
                if (departmentKey.HasValue) HHuser.GroupKeys.Add(departmentKey.Value);


            }
            catch (System.Exception ex)
            {
                GlobalLogger.Logger.Error(ex.Message);
                return null;
            }

            return HHuser;

        }
        private string? GetSafeString(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value
                ? row[columnName].ToString()
                : null;
        }


        private ADUser ParseADUser(DataRow userrow)
        {
            try
            {
                ADUser user = new ADUser
                {
                    Username = GetSafeString(userrow, "adusername"),
                    Name = GetSafeString(userrow, "name"),
                    Firstname = GetSafeString(userrow, "FirstName"),
                    Lastname = GetSafeString(userrow, "LastName"),
                    Email = GetSafeString(userrow, "Email"),
                    BadgeID = GetSafeString(userrow, "ActivateBadgeNumber"),
                    Role = GetSafeString(userrow, "Role"),
                    Department = GetSafeString(userrow, "Department"),
                    Manager = GetSafeString(userrow, "Manager"),
                };
                user.ActivateUser = ParseActivateUser(userrow);

                user.HHUser = ParseHHUser(userrow);
                return user;

            }
            catch (Exception e) {

                GlobalLogger.Logger.Error(e.ToString());
                return null;

            }

        }
        internal void InsertAdUser(ADUser user)
        {
            try
            {
                using (var cmd = new SqlCommand("dbo.InsertAdUser", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@username", user.Username);
                    cmd.Parameters.AddWithValue("@name", user.Name);
                    cmd.Parameters.AddWithValue("@role", user.Role);
                    cmd.Parameters.AddWithValue("@department", user.Department);
                    cmd.Parameters.AddWithValue("@ExpirationDate", user.ExpirationDate == null ? (object)DBNull.Value : user.ExpirationDate);
                    cmd.Parameters.AddWithValue("@Manager", user.Manager);
                    cmd.Parameters.AddWithValue("@tag", user.BadgeID);
                    cmd.Parameters.AddWithValue("@FirstName", user.Firstname);
                    cmd.Parameters.AddWithValue("@LastName", user.Lastname);
                    cmd.Parameters.AddWithValue("@employeenumber", user.EmployeeNumber);
                    cmd.Parameters.AddWithValue("@email", user.Email);


                    cmd.ExecuteNonQuery();
                }

            }

            catch (Exception ex)
            {
                GlobalLogger.Logger.Error(ex.Message);
                Console.Read();

            }
        }
        public void Dispose()
        {
            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }
        }
        internal ADUser SearchUserFromHH(string badgeNum)
        {
            ADUser match = new ADUser();


            string query = Environment.GetEnvironmentVariable("GET_HHUSER_CENTRAK_QUERY").Replace("@BADGENUM", badgeNum);
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            
                            int? itemID = !reader.IsDBNull(reader.GetOrdinal("id"))
                                ? reader.GetInt32(reader.GetOrdinal("id"))
                                : (int?)null;

                            int? roleKey = !reader.IsDBNull(reader.GetOrdinal("role_id"))
                                ? reader.GetInt32(reader.GetOrdinal("role_id"))
                                : (int?)null;

                            string? username = !reader.IsDBNull(reader.GetOrdinal("external_staff_identifier"))
                                ? reader.GetString(reader.GetOrdinal("external_staff_identifier"))
                                : null;

                            List<int>? groupKeys = !reader.IsDBNull(reader.GetOrdinal("group_ids"))
                                ? reader.GetString(reader.GetOrdinal("group_ids")).Split(',').Select(int.Parse).ToList()
                                : new List<int>(); 

                            match.HHUser = new HHUser(itemID, roleKey, groupKeys);
                           
                            match.Username = username;
                            if (string.IsNullOrEmpty(match.Username))
                            {
                                match.Username = "he who must not be named";
                            }
                            GlobalLogger.Logger.Debug("Found Hand Hygiene user with conflicting badge: " + username + " with badge: " + badgeNum + "with itemid: " + match.HHUser.ItemID);
                        }
                        else
                        {
                            GlobalLogger.Logger.Debug("Did not find any Hand Hygiene user with conflicting badge.");
                        }

                    }
                }
            }
            catch (Exception ex) {
                GlobalLogger.Logger.Error(ex.Message);
            }
          
            return match;
        }
        internal ADUser SearchUserFromActivate(string badgeNum)
        {
            ADUser match = new ADUser();


            string query = Environment.GetEnvironmentVariable("GET_ACTIVATEUSER_CENTRAK_QUERY").Replace("@BADGENUM", badgeNum);
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int? itemID = !reader.IsDBNull(reader.GetOrdinal("id"))
                            ? reader.GetInt32(reader.GetOrdinal("id"))
                            : (int?)null;
                        string? role = !reader.IsDBNull(reader.GetOrdinal("description"))
                            ? reader.GetString(reader.GetOrdinal("description"))
                            : null;

                        string? username = !reader.IsDBNull(reader.GetOrdinal("system_id"))
                            ? reader.GetString(reader.GetOrdinal("system_id")).Replace("-AD", "")
                            : null;

                        int? departmentKey = !reader.IsDBNull(reader.GetOrdinal("department_id"))
                            ? reader.GetInt32(reader.GetOrdinal("department_id"))
                            : (int?)null;

                        List<int>? groupKeys = !reader.IsDBNull(reader.GetOrdinal("group_ids"))
                            ? reader.GetString(reader.GetOrdinal("group_ids")).Split(',').Select(int.Parse).ToList()
                            : new List<int>();
                        match.ActivateUser = new ActivateUser(itemID, role, departmentKey, groupKeys);
                        match.Username = username;
                        GlobalLogger.Logger.Debug("Found Activate user with conflicting badge: " + username + " with badge: " + badgeNum + "with itemid: " + match.ActivateUser.ItemID);
                    }
                    else
                    {
                        GlobalLogger.Logger.Debug("Did not find any Activate user with conflicting badge.");
                    }
                }
            }
            return match;
        }


        internal async Task<int?> GetActivateItem(string username)
        {
            int? itemid = null;

            string query = Environment.GetEnvironmentVariable("GET_ACTIVATEUSER_ID_CENTRAK_QUERY").Replace("@USERNAME", username);
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {

                        itemid = !reader.IsDBNull(reader.GetOrdinal("id"))
                                ? reader.GetInt32(reader.GetOrdinal("id"))
                                : (int?)null;

                    }
                }
            }

            return itemid;
        }
        internal async Task<int?> GetHHItem(string username)
        {
            int? itemid = null;
            string query = Environment.GetEnvironmentVariable("GET_HHUSER_ID_CENTRAK_QUERY").Replace("@USERNAME", username);
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        itemid = !reader.IsDBNull(reader.GetOrdinal("id"))
                                ? reader.GetInt32(reader.GetOrdinal("id"))
                                : (int?)null;

                    }

                }
            }
            return itemid;
        }
        internal DataSet GetIPACReport()
        {

            string query = Environment.GetEnvironmentVariable("IPAC_REPORT_QUERY");
            string query2 = Environment.GetEnvironmentVariable("IPAC_REPORT_QUERY_WEEKLY");

            Console.WriteLine("Got the query:" + query);

            SqlCommand command = new SqlCommand(query, connection);
            command.CommandTimeout = 120;

            SqlCommand command2 = new SqlCommand(query2, connection);
            command2.CommandTimeout = 120; 

            SqlDataAdapter adapter = new SqlDataAdapter(command);

            Console.WriteLine("made the adapter");
            DataSet ds = new DataSet();
            try
            {
                adapter.Fill(ds, "Monthly");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw new Exception("failed to fill data set");
            }
            Console.WriteLine("Filled adapter");

            return ds;

        }
        private async Task TruncateTablePulse()
        {
            string truncateQuery = Environment.GetEnvironmentVariable("TRUNCATE_PULSE_STAGING");
            using (SqlCommand command = new SqlCommand(truncateQuery, connection))
            {
                try
                {
                 
                    await command.ExecuteNonQueryAsync();

                }
                catch (SqlException ex)
                {
                    GlobalLogger.Logger.Error($"SQL Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                
                    GlobalLogger.Logger.Error($"General Error: {ex.Message}");
                }
            }
        }
        private async Task TruncateStationTable()
        {
            string truncateQuery = Environment.GetEnvironmentVariable("TRUNCATE_PULSESTATION_STAGING");
            using (SqlCommand command = new SqlCommand(truncateQuery, connection))
            {
                try
                {
                    await command.ExecuteNonQueryAsync();

                }
                catch (SqlException ex)
                {
                    GlobalLogger.Logger.Error($"SQL Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    GlobalLogger.Logger.Error($"General Error: {ex.Message}");
                }
            }
        }

        internal async Task TruncateTableAD()
        {
            string truncateQuery = Environment.GetEnvironmentVariable("TRUNCATE_AD_STAGING");
            using (SqlCommand command = new SqlCommand(truncateQuery, connection))
            {
                try
                {
                    await command.ExecuteNonQueryAsync();

                }
                catch (SqlException ex)
                {
                    GlobalLogger.Logger.Error($"SQL Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    GlobalLogger.Logger.Error($"General Error: {ex.Message}");
                }
            }
        }

        //this function takes an excel sheet from the pulse website and inserts into the HILROM database it does not have 
        internal async Task InsertFirmwareReport(string package)
        {
            await TruncateTablePulse();
            var parts = package.Split(new[] { "<Excel>" }, StringSplitOptions.None);
            var innerParts = parts[1].Split(new[] { "</Excel>" }, StringSplitOptions.None);

            string csvContent = innerParts[0].Trim();
            var csvLines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            await InsertPulse("InsertFirmwareData", csvLines);




        }

        internal async Task InsertDeadBatteryReport(string package)
        {
            var parts = package.Split(new[] { "<Excel>" }, StringSplitOptions.None);
            var innerParts = parts[1].Split(new[] { "</Excel>" }, StringSplitOptions.None);

            string csvContent = innerParts[0].Trim();
            var csvLines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            await InsertPulseBattery("InsertDeadBatteryData", csvLines);

        }
        internal async Task InsertStationReport(string package)
        {
            //await TruncateTablePulse();
            //this is needs to be more clever I'm not sure how the fuck to do that but I'm leaving this here for someone not working on a friday

            var parts = package.Split(new[] { "<Excel>" }, StringSplitOptions.None);
            var innerParts = parts[1].Split(new[] { "</Excel>" }, StringSplitOptions.None);

            string csvContent = innerParts[0].Trim();
            var csvLines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            await InsertPulseInfra("InsertStationData", csvLines);




        }

        private async Task InsertPulseInfra(string storedprocedure, string[] csvLines)
        {
            await TruncateStationTable();
            foreach (var line in csvLines.Skip(3)) 
            {
                var values = line.Split(',');

                string tagId = values[0];
                string monitorId = values[1];
                string monitorLocation = values[2];
                string starAddress = values[3];
                string good = values[4];
                string batteryReplacement = values[5];
                string lessThan90Days = values[6];
                string lessThan30Days = values[7];
                string dateLastSeenByNetwork = values[8];
                string modelItem = values[9];
                string firmwareVersion = values[10];
                string offline = values[11];
                string batteryReplacedOn = values[12];
                string comment = values.Length > 13 ? values[13] : null;

                using (SqlCommand command = new SqlCommand(storedprocedure, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@TAG_ID", string.IsNullOrEmpty(tagId) ? DBNull.Value : (object)tagId);
                    command.Parameters.AddWithValue("@Monitor_Location", string.IsNullOrEmpty(monitorLocation) ? DBNull.Value : (object)monitorLocation);
                    command.Parameters.AddWithValue("@Monitor_ID", string.IsNullOrEmpty(monitorId) ? DBNull.Value : (object)Convert.ToInt64(monitorId));
                    command.Parameters.AddWithValue("@Star_Address", string.IsNullOrEmpty(starAddress) ? DBNull.Value : (object)starAddress);
                    command.Parameters.AddWithValue("@Good", string.IsNullOrEmpty(good) ? DBNull.Value : (object)good);
                    command.Parameters.AddWithValue("@Battery_Replacement_Indication", string.IsNullOrEmpty(batteryReplacement) ? DBNull.Value : (object)batteryReplacement);
                    command.Parameters.AddWithValue("@Less_than_90_Days", string.IsNullOrEmpty(lessThan90Days) ? DBNull.Value : (object)lessThan90Days);
                    command.Parameters.AddWithValue("@Less_than_30_Days", string.IsNullOrEmpty(lessThan30Days) ? DBNull.Value : (object)lessThan30Days);
                    command.Parameters.AddWithValue("@Date_Last_Seen_By_Network", string.IsNullOrEmpty(dateLastSeenByNetwork) ? DBNull.Value : (object)dateLastSeenByNetwork);
                    command.Parameters.AddWithValue("@Model_Item", string.IsNullOrEmpty(modelItem) ? DBNull.Value : (object)modelItem);
                    command.Parameters.AddWithValue("@Offline", string.IsNullOrEmpty(offline) ? DBNull.Value : (object)offline);
                    command.Parameters.AddWithValue("@Battery_Replaced_On", string.IsNullOrEmpty(batteryReplacedOn) ? DBNull.Value : (object)batteryReplacedOn);
                    command.Parameters.AddWithValue("@Comment", string.IsNullOrEmpty(comment) ? DBNull.Value : (object)comment);
                    command.Parameters.AddWithValue("@Firmware", string.IsNullOrEmpty(firmwareVersion) ? DBNull.Value : (object)firmwareVersion);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task InsertPulseBattery(string storedprocedure, string[] csvLines)
        {
            foreach (var line in csvLines.Skip(3)) 
            {
                var values = line.Split(',');
                string tagId = values[0];
                string monitorLocation = values[1];
                string monitorId = values[2];
                string BatteryReplacementIndication = values[3];
                string dateLastSeenByMonitor = values[4];
                string dateLastSeenByNetwork = values[5];
                string modelItem = values[6];
                string firmwareVersion = values[7];
                string offline = values[8];
                string comment = values.Length > 14 ? values[9] : null;

                using (SqlCommand command = new SqlCommand(storedprocedure, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@TAG_ID", string.IsNullOrEmpty(tagId) ? DBNull.Value : (object)Convert.ToInt64(tagId));
                    command.Parameters.AddWithValue("@Monitor_Location", string.IsNullOrEmpty(monitorLocation) ? DBNull.Value : (object)monitorLocation);
                    command.Parameters.AddWithValue("@Monitor_ID", string.IsNullOrEmpty(monitorId) ? DBNull.Value : (object)Convert.ToInt64(monitorId));
                    command.Parameters.AddWithValue("@Date_Last_Seen_By_Monitor", string.IsNullOrEmpty(dateLastSeenByMonitor) ? DBNull.Value : (object)dateLastSeenByMonitor);
                    command.Parameters.AddWithValue("@Date_Last_Seen_By_Network", string.IsNullOrEmpty(dateLastSeenByNetwork) ? DBNull.Value : (object)dateLastSeenByNetwork);
                    command.Parameters.AddWithValue("@Model_Item", string.IsNullOrEmpty(modelItem) ? DBNull.Value : (object)modelItem);
                    command.Parameters.AddWithValue("@Offline", string.IsNullOrEmpty(offline) ? DBNull.Value : (object)offline);
                    command.Parameters.AddWithValue("@Comment", string.IsNullOrEmpty(comment) ? DBNull.Value : (object)comment);
                    command.Parameters.AddWithValue("@Firmware", string.IsNullOrEmpty(firmwareVersion) ? DBNull.Value : (object)firmwareVersion);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        private async Task InsertPulse(string storedprocedure, string[] csvLines)
        {
            
            foreach (var line in csvLines.Skip(3))
            {
                var values = line.Split(',');

                string tagId = values[0];
                string monitorLocation = values[1];
                string monitorId = values[2];
                string starAddress = values[3];
                string good = values[4];
                string batteryReplacement = values[5];
                string lessThan90Days = values[6];
                string lessThan30Days = values[7];
                string dateLastSeenByMonitor = values[8];
                string dateLastSeenByNetwork = values[9];
                string modelItem = values[10];
                string firmwareVersion = values[11];
                string offline = values[12];
                string batteryReplacedOn = values[13];
                string comment = values.Length > 14 ? values[14] : null;

                using (SqlCommand command = new SqlCommand(storedprocedure, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@TAG_ID", string.IsNullOrEmpty(tagId) ? DBNull.Value : (object)Convert.ToInt64(tagId));
                    command.Parameters.AddWithValue("@Monitor_Location", string.IsNullOrEmpty(monitorLocation) ? DBNull.Value : (object)monitorLocation);
                    command.Parameters.AddWithValue("@Monitor_ID", string.IsNullOrEmpty(monitorId) ? DBNull.Value : (object)Convert.ToInt64(monitorId));
                    command.Parameters.AddWithValue("@Star_Address", string.IsNullOrEmpty(starAddress) ? DBNull.Value : (object)starAddress);
                    command.Parameters.AddWithValue("@Good", string.IsNullOrEmpty(good) ? DBNull.Value : (object)good);
                    command.Parameters.AddWithValue("@Battery_Replacement_Indication", string.IsNullOrEmpty(batteryReplacement) ? DBNull.Value : (object)batteryReplacement);
                    command.Parameters.AddWithValue("@Less_than_90_Days", string.IsNullOrEmpty(lessThan90Days) ? DBNull.Value : (object)lessThan90Days);
                    command.Parameters.AddWithValue("@Less_than_30_Days", string.IsNullOrEmpty(lessThan30Days) ? DBNull.Value : (object)lessThan30Days);
                    command.Parameters.AddWithValue("@Date_Last_Seen_By_Monitor", string.IsNullOrEmpty(dateLastSeenByMonitor) ? DBNull.Value : (object)dateLastSeenByMonitor);
                    command.Parameters.AddWithValue("@Date_Last_Seen_By_Network", string.IsNullOrEmpty(dateLastSeenByNetwork) ? DBNull.Value : (object)dateLastSeenByNetwork);
                    command.Parameters.AddWithValue("@Model_Item", string.IsNullOrEmpty(modelItem) ? DBNull.Value : (object)modelItem);
                    command.Parameters.AddWithValue("@Offline", string.IsNullOrEmpty(offline) ? DBNull.Value : (object)offline);
                    command.Parameters.AddWithValue("@Battery_Replaced_On", string.IsNullOrEmpty(batteryReplacedOn) ? DBNull.Value : (object)batteryReplacedOn);
                    command.Parameters.AddWithValue("@Comment", string.IsNullOrEmpty(comment) ? DBNull.Value : (object)comment);
                    command.Parameters.AddWithValue("@Firmware", string.IsNullOrEmpty(firmwareVersion) ? DBNull.Value : (object)firmwareVersion);

                    await command.ExecuteNonQueryAsync();
                }


            }
        }

        public async Task<Pod> GetPod(string floor, string unit)
        {

            try
            {
                string query = Environment.GetEnvironmentVariable("POD_QUERY").Replace("@FLOOR", floor).Replace("@UNIT", unit);
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query, connection);
                DataSet set = new DataSet();
                sqlDataAdapter.Fill(set);
                Pod pod = await ParsePod(set);

                return pod;

            }
            catch (Exception ex)

            {
                GlobalLogger.Logger.Debug("Error:" + ex);
                throw ex;
            }


        }

        public async Task<Pod> GetOperationalPod(string floor, string unit)
        {

            try
            {
                string query = Environment.GetEnvironmentVariable("OPERATIONAL_POD_QUERY").Replace("@FLOOR", floor).Replace("@UNIT", unit);
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query, connection);
                DataSet set = new DataSet();
                sqlDataAdapter.Fill(set);
                Pod pod = await ParsePod(set);

                return pod;

            }
            catch (Exception ex)

            {
                GlobalLogger.Logger.Debug("Error:" + ex);
                throw ex;
            }


        }

        public async Task ArchiveStation(string stationKey)
        {
            try
            {
                string query = Environment.GetEnvironmentVariable("ARCHIVE_STATION_QUERY")?.Replace("@KEY", stationKey);

                GlobalLogger.Logger.Debug("QUERY FOR ARCHIVE STATION: " + query);
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync(); 
                }
            }

            catch (Exception ex)
            {

                
                GlobalLogger.Logger.Debug("Error: " + ex);
                throw;
            }
        }
        public async Task<Dictionary<int,Note>> GetNotesForStation(string stationID)
        {

            try
            {
                string query = Environment.GetEnvironmentVariable("NOTES_QUERY").Replace("@STATION_ID", stationID);
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query, connection);
                DataSet set = new DataSet();
                sqlDataAdapter.Fill(set);
                Dictionary<int, Note> notes = ParseNotes(set);

                return notes;

            }
            catch (Exception ex)

            {
                GlobalLogger.Logger.Debug("Error:" + ex);
                throw ex;
            }


        }
        internal DataSet GetBatteriesSummarReport()
        {
            try {
                string query = Environment.GetEnvironmentVariable("BATTERY_SUMMARY_QUERY");

                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query,connection);
                DataSet set = new DataSet();
                sqlDataAdapter.Fill(set);
                return set;
            
            
            }
            catch (Exception ex)

            {
                GlobalLogger.Logger.Debug("Error:" + ex);
                throw ex;
            }

        }

        private async Task<Pod> ParsePod(DataSet set)
        {
            Pod pod = new Pod();
            foreach (DataRow dataRow in set.Tables[0].Rows)
            {
                HHStation.Coords coords = new HHStation.Coords(int.Parse(dataRow["XCoord"].ToString()), int.Parse(dataRow["YCoord"].ToString()));

                Dictionary<int, Note> notes = await GetNotesForStation(dataRow["Station_Key"].ToString());

                HHStation station = new HHStation(
                    dataRow["Name"].ToString(),
                    dataRow["Location"].ToString(),
                    dataRow["Type"].ToString(),
                    Convert.ToBoolean(dataRow["Status"]),
                    int.Parse(dataRow["StationID"].ToString()),
                    notes,
                    coords,
                    double.Parse(dataRow["ModelResult"].ToString()),
                    int.Parse(dataRow["Station_Key"].ToString())
                    );

                GlobalLogger.Logger.Debug("this is the online status parsed: " + Convert.ToBoolean(dataRow["Status"]));

                pod.HHStations.Add(station.StationKey, station);
            }
            return pod;
        }

        private static Dictionary<int, Note> ParseNotes(DataSet set)
        {
            Dictionary<int, Note> notes  = new Dictionary<int, Note>();

            foreach(DataRow dataRow in set.Tables[0].Rows)
            {
                notes.Add(
                 int.Parse(dataRow["Note_Key"].ToString()),
                 new Note(
                     dataRow["Content"].ToString(),
                     DateTime.Parse(dataRow["CreateDate"].ToString()),
                     dataRow["Author"].ToString(),
                     int.Parse(dataRow["Note_Key"].ToString())
                 )
 );
            }

            return notes;
        }

    }


}
        

