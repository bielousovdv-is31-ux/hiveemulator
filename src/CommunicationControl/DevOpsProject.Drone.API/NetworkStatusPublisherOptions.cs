using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Drone.API;

public class NetworkStatusPublisherOptions
{
    [Required]
    public TimeSpan Delay { get; set; }
}
