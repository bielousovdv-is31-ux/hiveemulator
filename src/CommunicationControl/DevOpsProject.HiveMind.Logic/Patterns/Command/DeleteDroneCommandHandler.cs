using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class DeleteDroneCommandHandler(IDroneService droneService) : ICommandHandler<DeleteDroneCommand>
{
    public async Task HandleAsync(DeleteDroneCommand command)
    {
        await droneService.DisconnectDroneAsync(command.DroneId);
    }
}
