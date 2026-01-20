using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class AddDroneCommandHandler(IDroneService droneService) : ICommandHandler<AddDroneCommand>
{
    public async Task HandleAsync(AddDroneCommand command)
    {
        await droneService.ConnectDroneAsync(command.IpAddress, command.GrpcPort);
    }
}
