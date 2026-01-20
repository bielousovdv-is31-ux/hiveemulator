namespace DevOpsProject.Shared.Models.HiveMindCommands;

public sealed class SimulateDeadConnectionCommand : HiveMindCommand
{
    public string Connection1Name { get; set; }
    public string Connection2Name { get; set; }
    public TimeSpan? Duration { get; set; }
}
