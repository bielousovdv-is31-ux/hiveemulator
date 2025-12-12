using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class SimulateBadDroneCommandHandler(IDroneService droneService) : ICommandHandler<SimulateBadDroneCommand>
{
    public async Task HandleAsync(SimulateBadDroneCommand command)
    {
        await droneService.SimulateBadDroneAsync(command);
    }
}