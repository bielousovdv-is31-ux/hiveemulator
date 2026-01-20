namespace DevOpsProject.Shared.Models.HiveMindCommands;

public class SimulateDroneStoppedOperatingCommand : HiveMindCommand
{
    public string DroneId { get; set; }
    public TimeSpan? Duration { get; set; }
}
