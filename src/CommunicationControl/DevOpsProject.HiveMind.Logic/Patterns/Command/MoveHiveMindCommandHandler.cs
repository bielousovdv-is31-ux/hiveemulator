using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command
{
    public class MoveHiveMindCommandHandler : ICommandHandler<MoveHiveMindCommand>
    {
        private readonly IDroneService _droneService;

        public MoveHiveMindCommandHandler(IDroneService droneService)
        {
            _droneService = droneService;
        }

        public async Task HandleAsync(MoveHiveMindCommand command)
        {
            await _droneService.MoveToLocationAsync(command.Destination);
        }
    }
}
