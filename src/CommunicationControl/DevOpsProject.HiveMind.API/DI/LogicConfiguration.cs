using DevOpsProject.HiveMind.Logic.Patterns.Command;
using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Patterns.Factory;
using DevOpsProject.HiveMind.Logic.Patterns.Factory.Interfaces;
using DevOpsProject.HiveMind.Logic.Services;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.API.DI
{
    public static class LogicConfiguration
    {
        public static IServiceCollection AddHiveMindLogic(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ICommandHandler<MoveHiveMindCommand>, MoveHiveMindCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<StopHiveMindCommand>, StopHiveMindCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<AddInterferenceToHiveMindCommand>, AddInterferenceToHiveMindCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<DeleteInterferenceFromHiveMindCommand>, DeleteInterferenceFromHiveMindCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<AddDroneCommand>, AddDroneCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<DeleteDroneCommand>, DeleteDroneCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<SimulateDeadConnectionCommand>, SimulateDeadConnectionCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<StopDeadConnectionSimulationCommand>, StopDeadConnectionSimulationCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<SimulateDroneStoppedOperatingCommand>, SimulateDroneStoppedOperatingCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<StopDroneStoppedOperatingSimulationCommand>, StopDroneStoppedOperatingSimulationCommandHandler>();
            serviceCollection.AddTransient<ICommandHandlerFactory, CommandHandlerFactory>();

            serviceCollection.AddTransient<IHiveMindService, HiveMindService>();
            
            serviceCollection.AddSingleton<IDroneTelemetryService, DroneTelemetryService>();
            serviceCollection.AddSingleton<IDroneService, DroneService>();
            serviceCollection.AddSingleton<ISimulationService, SimulationService>();

            return serviceCollection;
        }
    }
}
