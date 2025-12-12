using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class SimulateBadConnectionCommandHandler(IDroneService droneService) : ICommandHandler<SimulateBadConnectionCommand>
{
    public async Task HandleAsync(SimulateBadConnectionCommand command)
    {
        await droneService.SimulateBadConnectionAsync(command);
    }
}
