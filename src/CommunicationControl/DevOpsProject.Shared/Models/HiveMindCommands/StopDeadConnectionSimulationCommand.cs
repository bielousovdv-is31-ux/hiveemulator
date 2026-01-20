namespace DevOpsProject.Shared.Models.HiveMindCommands;

public class StopDeadConnectionSimulationCommand : HiveMindCommand
{
    public string Connection1Name { get; set; }
    public string Connection2Name { get; set; }
}
