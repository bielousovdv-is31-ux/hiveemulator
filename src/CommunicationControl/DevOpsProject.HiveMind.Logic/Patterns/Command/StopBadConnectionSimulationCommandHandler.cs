using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class StopBadConnectionSimulationCommandHandler(IDroneService droneService) : ICommandHandler<StopBadConnectionSimulationCommand>
{
    public async Task HandleAsync(StopBadConnectionSimulationCommand command)
    {
        await droneService.StopBadConnectionSimulationAsync(command);
    }
}
