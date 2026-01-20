using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Drone.API;

public class DroneTelemetryPublisherOptions
{
    [Required]
    public TimeSpan Delay { get; set; }
}
