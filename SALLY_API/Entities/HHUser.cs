
using System.Text.Json;

namespace SALLY_API.Entities
{
    public class HHUser : ISystemUserInfo
    {
        public int primarykey { get; set; }
        public int? ItemID { get; set; }
        public List<int>? GroupKeys { get; set ; } = new List<int>();

        public int? RoleKey { get; set; }

        public HHUser() { }
        public HHUser(int? itemID, int? roleKey, List<int> groupKeys)
        {
            ItemID = itemID;
            RoleKey = roleKey;
            GroupKeys = groupKeys;
        }

        //// JSON constructor
        public HHUser(string json)
        {
            // Deserialize the JSON into a temporary instance of SystemUserInfo
            var deserializedUserInfo = JsonSerializer.Deserialize<HHUser>(json);

            // Copy properties from the deserialized object to this instance
            if (deserializedUserInfo != null)
            {
                ItemID = deserializedUserInfo.ItemID;
                RoleKey = deserializedUserInfo.RoleKey;
                GroupKeys = deserializedUserInfo.GroupKeys ?? new List<int>();
            }
            else
            {
                throw new ArgumentException("Invalid JSON provided.");
            }
        }

        public string ToString()
        {
            return "ItemID: " + ItemID + "\n" +
                   "Role Key: " + RoleKey + "\n" +
                   "Group Keys:\n" +
                   string.Join("\n", GroupKeys.Select(group => "\t- " + group));
        }
    public bool IsEqualTo(ISystemUserInfo? otherUser)
        {
            HHUser other = (HHUser)otherUser;

            if (
                RoleKey == other.RoleKey &&
                GroupKeys.All(other.GroupKeys.Contains) && other.GroupKeys.All(GroupKeys.Contains)
                )
            {
                return true;
            }

            return false;
        }
    }
}
