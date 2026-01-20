namespace DevOpsProject.Shared.Models.HiveMindCommands;

public class AddDroneCommand : HiveMindCommand
{
    public string IpAddress { get; set; }
    public int GrpcPort { get; set; }
}
