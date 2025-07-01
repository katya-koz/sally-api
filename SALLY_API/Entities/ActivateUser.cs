
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace SALLY_API.Entities
{
    public class ActivateUser : ISystemUserInfo
    {   

        public int primarykey { get; set; }
        public int? ItemID { get; set; }
        public List<int>? GroupKeys { get; set; } = new List<int>();
        public int? DepartmentKey { get; set; }
        public string? Role { get; set; }

        public ActivateUser() {
            GroupKeys.Add(1);
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Test")
            {
                GroupKeys.Add(15); //this is for test

            }
            else
            {
                GroupKeys.Add(31); //this is for prod 
            }

            }
            public string ToString()
        {
            return "ItemID: " + ItemID + "\n" +
                   "Role: " + Role + "\n" +
                   "Group Keys:\n" +
                   string.Join("\n", GroupKeys.Select(group => "\t- " + group));
        }

        public ActivateUser(int? itemID, string? role,int? departmentKey, List<int> groupKeys)
        {
            ItemID = itemID;
            Role = role;
            DepartmentKey = departmentKey;
            GroupKeys = groupKeys;
            if(!GroupKeys.Contains(1)) GroupKeys.Add(1); //default group is id 1

        }

        //// JSON constructor
        public ActivateUser(string json)
        {
            // Deserialize the JSON into a temporary instance of SystemUserInfo
            var deserializedUserInfo = JsonSerializer.Deserialize<ActivateUser>(json);

            // Copy properties from the deserialized object to this instance
            if (deserializedUserInfo != null)
            {
                ItemID = deserializedUserInfo.ItemID;
                Role = deserializedUserInfo.Role;
                DepartmentKey = deserializedUserInfo.DepartmentKey;
                GroupKeys = deserializedUserInfo.GroupKeys ?? new List<int>();
            }
            else
            {
                throw new ArgumentException("Invalid JSON provided.");
            }
        }

        public bool IsEqualTo(ISystemUserInfo? otherUser)
        {
            ActivateUser other = (ActivateUser)otherUser;
            //we should probably add a check for department key as well.
            
            if (GroupKeys.All(other.GroupKeys.Contains) && other.GroupKeys.All(GroupKeys.Contains))
            {
                if (string.Equals(other.Role ?? "", Role ?? "", StringComparison.Ordinal))
                {
                    GlobalLogger.Logger.Debug("Conflicting user found to be equal to target");
                    return true;
                }
            }

            GlobalLogger.Logger.Debug("Conflicting user found to be different to target");
            return false;
        }
    }
}
