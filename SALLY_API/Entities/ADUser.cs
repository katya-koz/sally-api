using System.Text.Json;

namespace SALLY_API.Entities
{
    /*
      made the HHUser and ActivateUser public, should review protection levels 
     */ 
     
    public class ADUser : IEntity, IComparable<ADUser>
    {
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? Department { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? Manager { get; set; }
        public string? BadgeID { get; set; }

        public string? Email { get; set; }

        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? EmployeeNumber { get; set; }

        public HHUser? HHUser { get; set; }
        public ActivateUser? ActivateUser { get; set; }

        public ADUser()
        {
            HHUser = new HHUser();
            ActivateUser = new ActivateUser();
        }

        public int CompareTo(ADUser? other)
        {
            // return 1 if equal
            // 0 ow

            return 0;
        }
    
        public override string ToString(){
            return ("===============ADUSER===============" + "\n" +
            "Username: " + Username + "\n" +
            "Name: " + Firstname + " " + Lastname + "\n" +
            "Role: " + Role + "\n" +
            "Department: " + Department + "\n" +
            "Manager: " + Manager + "\n" +
            "BadgeID: " + BadgeID + "\n" +
            "Email: " + Email + "\n" +
            "Employee Number: " + EmployeeNumber +"\n" +
                HHUser.ToString() + "\n" +
                ActivateUser.ToString());

        }
        public ADUser(string json)
        {
            // Deserialize JSON into the current instance of ADUser
            var deserializedUser = JsonSerializer.Deserialize<ADUser>(json);

            // Copy properties from the deserialized object to this instance
            if (deserializedUser != null)
            {
                Username = deserializedUser.Username;
                Name = deserializedUser.Name;
                Role = deserializedUser.Role;
                Department = deserializedUser.Department;
                ExpirationDate = deserializedUser.ExpirationDate ?? DateTime.MinValue;
                Manager = deserializedUser.Manager;
                BadgeID = deserializedUser.BadgeID;
                Email = deserializedUser.Email;
                Firstname = deserializedUser.Firstname;
                Lastname = deserializedUser.Lastname;
                EmployeeNumber = deserializedUser.EmployeeNumber;
                HHUser = deserializedUser.HHUser ?? new HHUser();
                ActivateUser = deserializedUser.ActivateUser ?? new ActivateUser();
            }
            else
            {
                throw new ArgumentException("Invalid JSON provided.");
            }
        }

        //public bool IsEqualTo(ADUser other)
        //{
        //    if (BadgeID == other.BadgeID &&
        //        Username == other.Username &&
        //        HHUser.IsEqualTo(other.HHUser) &&
        //        ActivateUser.IsEqualTo(other.ActivateUser))
        //    {
        //        return true;

        //    }

        //    return false;
        //}
        //private string ExtractValue(string json, string key)
        //{
        //    string searchKey = $"\"{key}\":";
        //    int startIndex = json.IndexOf(searchKey);
        //    if (startIndex == -1) return "";

        //    startIndex += searchKey.Length;
        //    int endIndex = json.IndexOf(",", startIndex);
        //    if (endIndex == -1) endIndex = json.IndexOf("}", startIndex);
        //    if (endIndex == -1) return "";

        //    string value = json.Substring(startIndex, endIndex - startIndex).Trim().Trim('"');

        //    return value;
        //}

        
    }
}
