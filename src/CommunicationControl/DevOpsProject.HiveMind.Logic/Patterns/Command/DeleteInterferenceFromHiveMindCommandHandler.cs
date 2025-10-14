using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command
{
    public class DeleteInterferenceFromHiveMindCommandHandler : ICommandHandler<DeleteInterferenceFromHiveMindCommand>
    {
        private readonly IHiveMindService _hiveMindService;

        public DeleteInterferenceFromHiveMindCommandHandler(IHiveMindService hiveMindService)
        {
            _hiveMindService = hiveMindService;
        }

        public async Task HandleAsync(DeleteInterferenceFromHiveMindCommand command)
        {

            _hiveMindService.RemoveInterference(command.InterferenceId);
            await Task.CompletedTask;
        }
    }
}
