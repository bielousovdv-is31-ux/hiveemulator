using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command
{
    public class AddInterferenceToHiveMindCommandHandler : ICommandHandler<AddInterferenceToHiveMindCommand>
    {
        private readonly IHiveMindService _hiveMindService;

        public AddInterferenceToHiveMindCommandHandler(IHiveMindService hiveMindService)
        {
            _hiveMindService = hiveMindService;
        }

        public async Task HandleAsync(AddInterferenceToHiveMindCommand command)
        {

            _hiveMindService.AddInterference(command.Interference);
            await Task.CompletedTask;
        }
    }
}
