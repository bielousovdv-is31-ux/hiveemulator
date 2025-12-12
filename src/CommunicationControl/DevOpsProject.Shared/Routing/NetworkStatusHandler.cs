using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using Listener;
using ConnectionType = DevOpsProject.Shared.Enums.ConnectionType;
using ForeignConnection = DevOpsProject.Shared.Models.ForeignConnection;

namespace DevOpsProject.Shared.Routing;

public sealed class NetworkStatusHandler(IRouterService routerService) : IUdpMessageHandler<NetworkStatus>
{
    public Task HandleAsync(NetworkStatus message, CancellationToken token)
    {
        var previousConnection = routerService.GetConnectionOrNull(Connection.GetName(message.Id, (ConnectionType) message.Type));
        if (previousConnection == null)
        {
            return Task.CompletedTask;
        }

        var connection = previousConnection with
        {
            IpAddress = message.IpAddress,
            Http1Port = message.Http1Port,
            GrpcPort = message.GrpcPort,
            UdpPort = message.UdpPort,
            LastUpdatedAt = message.SentAt.ToDateTimeOffset()
        };
        
        _ = routerService.TryUpdateConnection(connection, message.Connections.Select(c => new ForeignConnection(c.Name, c.LastUpdatedAt.ToDateTimeOffset())));
        return Task.CompletedTask;
    }
}
