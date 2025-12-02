using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using Listener;
using ConnectionType = DevOpsProject.Shared.Enums.ConnectionType;

namespace DevOpsProject.Drone.API;

public sealed class NetworkStatusHandler(IRouterService routerService) : IUdpMessageHandler<NetworkStatus>
{
    public Task HandleAsync(NetworkStatus message, CancellationToken token)
    {
        var previousConnection = routerService.GetConnectionOrNull(Connection.GetName(message.Id, (ConnectionType) message.Type));
        Connection connection;
        if (previousConnection != null)
        {
            connection = previousConnection;
            previousConnection.IpAddress = message.IpAddress;
            previousConnection.Http1Port = message.Http1Port;
            previousConnection.GrpcPort = message.GrpcPort;
            previousConnection.UdpPort = message.UdpPort;
            previousConnection.PreviousLastUpdatedAt = previousConnection.LastUpdatedAt;
            previousConnection.LastUpdatedAt = message.SentAt.ToDateTimeOffset();
        }
        else
        {
            connection = new Connection(message.Id, (ConnectionType) message.Type, message.IpAddress, message.Http1Port, message.GrpcPort, message.UdpPort, message.SentAt.ToDateTimeOffset())
            {
                PreviousLastUpdatedAt = message.SentAt.ToDateTimeOffset()
            };
        }
        _ = routerService.TryUpdateConnection(connection, message.AliveConnectionNames.ToHashSet());
        
        return Task.CompletedTask;
    }
}
