using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class StopDroneStoppedOperatingSimulationCommandHandler(IDroneService droneService) : ICommandHandler<StopDroneStoppedOperatingSimulationCommand>
{
    public async Task HandleAsync(StopDroneStoppedOperatingSimulationCommand command)
    {
        await droneService.StopDroneStoppedOperatingSimulationAsync(command);
    }
}