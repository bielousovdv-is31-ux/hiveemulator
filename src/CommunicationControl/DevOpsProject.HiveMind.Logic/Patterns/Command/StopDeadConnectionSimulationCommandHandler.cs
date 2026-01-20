using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class StopDeadConnectionSimulationCommandHandler(IDroneService droneService) : ICommandHandler<StopDeadConnectionSimulationCommand>
{
    public async Task HandleAsync(StopDeadConnectionSimulationCommand command)
    {
        await droneService.StopDeadConnectionSimulationAsync(command);
    }
}
