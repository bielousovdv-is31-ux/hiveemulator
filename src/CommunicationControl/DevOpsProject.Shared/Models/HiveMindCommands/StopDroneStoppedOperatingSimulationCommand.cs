namespace DevOpsProject.Shared.Models.HiveMindCommands;

public sealed class StopDroneStoppedOperatingSimulationCommand : HiveMindCommand
{
    public string DroneId { get; set; }
}