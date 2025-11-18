using DevOpsProject.Drone.Logic.Services.Interfaces;
using DevOpsProject.Drone.Logic.Services.Results;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.Drone.Logic.Services;

public sealed class CommunicationService : ICommunicationService
{
    private readonly Dictionary<string, Connection> _connections = new Dictionary<string, Connection>();
    private readonly Lock _connectionsLock = new Lock();

    public Result Connect(Connection connection)
    {
        lock (_connectionsLock)
        {
            if (_connections.ContainsKey(connection.Name))
            {
                return Result.Error(ErrorType.AlreadyExists);
            }
            
            // TODO: ping.
            
            _connections.Add(connection.Name, connection);
            return Result.Success();
        }
    }
}
