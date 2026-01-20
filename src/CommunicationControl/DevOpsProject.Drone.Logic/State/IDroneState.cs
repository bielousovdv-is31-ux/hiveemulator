using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.Drone.Logic.State;

public interface IDroneState : ICloneable
{
    string Name { get; }
    string DroneId { get; }
    DroneType Type { get; }
    Location Location { get; set; }
    Location? Destination { get; set; }
    float Speed { get; set; }
    float Height { get; set; }
    DevOpsProject.Shared.Enums.DroneState State { get; set; }
}
