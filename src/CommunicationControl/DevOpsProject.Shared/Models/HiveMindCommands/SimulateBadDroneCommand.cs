namespace DevOpsProject.Shared.Models.HiveMindCommands;

public class SimulateBadDroneCommand : HiveMindCommand
{
    public string DroneId { get; set; }
    public TimeSpan? Duration { get; set; }
    public TimeSpan Latency { get; set; }
}
