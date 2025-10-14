using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Patterns.Factory.Interfaces;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models.HiveMindCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Patterns.Factory
{
    public class CommandHandlerFactory : ICommandHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CommandHandlerFactory> _logger;

        public CommandHandlerFactory(IServiceProvider serviceProvider, ILogger<CommandHandlerFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public ICommandHandler GetHandler(HiveMindCommand command)
        {
            ICommandHandler handler = null;
            var commandType = command.CommandType;
            switch (commandType)
            {
                case HiveMindState.Move:
                    handler = _serviceProvider.GetService<ICommandHandler<MoveHiveMindCommand>>();
                    break;
                case HiveMindState.Stop:
                    handler = _serviceProvider.GetService<ICommandHandler<StopHiveMindCommand>>();
                    break;
                case HiveMindState.SetInterference:
                    handler = _serviceProvider.GetService<ICommandHandler<AddInterferenceToHiveMindCommand>>();
                    break;
                case HiveMindState.DeleteInterference:
                    handler = _serviceProvider.GetService<ICommandHandler<DeleteInterferenceFromHiveMindCommand>>();
                    break;
                default:
                    _logger.LogError("Corresponding handler not found for command: {@command}", command);
                    throw new Exception($"Unsupported command occured, type: {command.CommandType}");
            }

            return handler;
        }
    }

}
