using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command
{
    public class StopHiveMindCommandHandler : ICommandHandler<StopHiveMindCommand>
    {
        private readonly IDroneService _droneService;

        public StopHiveMindCommandHandler(IDroneService droneService)
        {
            _droneService = droneService;
        }

        public async Task HandleAsync(StopHiveMindCommand command)
        {
            await _droneService.StopHiveMindMovingAsync(command.StopImmediately);
        }
    }
}
