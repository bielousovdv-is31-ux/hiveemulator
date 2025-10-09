using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models
{
    public class MoveHiveMindCommand
    {
        public State CommandType { get; set; }
        public Location Location { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
