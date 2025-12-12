namespace DevOpsProject.Shared.Models.HiveMindCommands;

public class StopBadConnectionSimulationCommand : HiveMindCommand
{
    public string Connection1Name { get; set; }
    public string Connection2Name { get; set; }
}
