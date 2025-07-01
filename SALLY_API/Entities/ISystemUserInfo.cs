namespace SALLY_API.Entities
{
    public interface ISystemUserInfo : IEntity
    {
        public int? ItemID { get; set; }
        public List<int>? GroupKeys { get; set; }

        public string ToString();

        public bool IsEqualTo(ISystemUserInfo? other);

    }
}
