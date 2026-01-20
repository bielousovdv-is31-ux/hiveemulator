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
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
            var handler = _serviceProvider.GetService(handlerType);
            
            if (handler == null)
            {
                _logger.LogError("Corresponding handler not found for command: {@command}", command);
                throw new Exception($"Unsupported command occured, type: {command.GetType()}");
            }

            return (ICommandHandler) handler;
        }
    }

}
