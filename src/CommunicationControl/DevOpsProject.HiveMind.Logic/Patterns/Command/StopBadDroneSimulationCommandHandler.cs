using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class StopBadDroneSimulationCommandHandler(IDroneService droneService) : ICommandHandler<StopBadDroneSimulationCommand>
{
    public async Task HandleAsync(StopBadDroneSimulationCommand simulationCommand)
    {
        await droneService.StopBadDroneSimulationAsync(simulationCommand);
    }
}