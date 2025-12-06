namespace DevOpsProject.Shared.Models.HiveMindCommands;

public sealed class DeleteDroneCommand : HiveMindCommand
{
    public string DroneId { get; set; }
    public bool Force { get; set; }
}