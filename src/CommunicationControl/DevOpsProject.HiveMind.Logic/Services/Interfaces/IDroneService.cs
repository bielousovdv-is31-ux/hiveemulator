using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces;

public interface IDroneService
{
    Task ConnectDroneAsync(string ipAddress, int port);
    Task DisconnectDroneAsync(string droneId, bool force);
    Task SimulateDeadConnectionAsync(SimulateDeadConnectionCommand command);
    Task StopDeadConnectionSimulationAsync(StopDeadConnectionSimulationCommand command);
}
