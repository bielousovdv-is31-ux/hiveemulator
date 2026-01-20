using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command;

public sealed class SimulateDroneStoppedOperatingCommandHandler(IDroneService droneService) : ICommandHandler<SimulateDroneStoppedOperatingCommand>
{
    public async Task HandleAsync(SimulateDroneStoppedOperatingCommand command)
    {
        await droneService.SimulateDroneStoppedOperatingAsync(command);
    }
}