namespace DevOpsProject.Shared.Models.HiveMindCommands;

public sealed class StopBadDroneSimulationCommand : HiveMindCommand
{
    public string DroneId { get; set; }
}