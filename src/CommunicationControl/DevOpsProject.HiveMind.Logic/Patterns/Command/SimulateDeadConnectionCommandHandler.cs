using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class SimulateDeadConnectionCommandHandler(IDroneService droneService) : ICommandHandler<SimulateBadConnectionCommand>
{
    public async Task HandleAsync(SimulateBadConnectionCommand command)
    {
        await droneService.SimulateDeadConnectionAsync(command);
    }
}
