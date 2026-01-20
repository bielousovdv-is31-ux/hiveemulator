using System.ComponentModel.DataAnnotations;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.Drone.Logic.State;

public class DroneInitialStateOptions
{
    [Required]
    public string Id { get; set; }
    public Location Location { get; set; }
    [EnumDataType(typeof(DroneType))]
    [DeniedValues(DroneType.Undefined)]
    public DroneType Type { get; set; } = DroneType.Striker;
}
