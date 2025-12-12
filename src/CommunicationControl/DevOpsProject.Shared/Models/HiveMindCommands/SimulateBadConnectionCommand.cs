namespace DevOpsProject.Shared.Models.HiveMindCommands;

public sealed class SimulateBadConnectionCommand : HiveMindCommand
{
    public string Connection1Name { get; set; }
    public string Connection2Name { get; set; }
    public TimeSpan? Duration { get; set; }
    public TimeSpan Latency { get; set; }
}
