namespace DevOpsProject.HiveMind.Logic.Services.Interfaces;

public interface IDroneService
{
    Task ConnectDroneAsync(string ipAddress, int port);
    Task DisconnectDroneAsync(string droneId);
}
