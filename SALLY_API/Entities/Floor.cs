using SALLY_API.Entities.Handsify;

namespace SALLY_API.Entities
{
    public class Floor
    {
        public List<Pod> Pods { get; set; }
        public Floor()
        {
            Pods = new List<Pod>();
        }
    }
}
