using DevOpsProject.HiveMind.Logic.Dto;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces;

public interface IDroneService
{
    Task ConnectDroneAsync(string ipAddress, int port);
    Task DisconnectDroneAsync(string droneId, bool force);
    Task SimulateBadConnectionAsync(SimulateBadConnectionCommand command);
    Task StopBadConnectionSimulationAsync(StopBadConnectionSimulationCommand command);
    Task SimulateBadDroneAsync(SimulateBadDroneCommand command);
    Task StopBadDroneSimulationAsync(StopBadDroneSimulationCommand simulationCommand);
    Task MoveToLocationAsync(Location destination);
    Task StopHiveMindMovingAsync(bool immediateStop);
    IReadOnlyList<DroneDto> GetDrones();
    DroneDetailsDto GetDrone(string id);
}
