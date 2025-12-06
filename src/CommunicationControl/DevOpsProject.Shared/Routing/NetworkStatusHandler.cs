using Common;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using Listener;
using Microsoft.Extensions.Logging;
using ConnectionType = DevOpsProject.Shared.Enums.ConnectionType;

namespace DevOpsProject.Shared.Routing;

public sealed class NetworkStatusHandler(IRouterService routerService, IUdpService udpService, ILogger<NetworkStatusHandler> logger) : IUdpMessageHandler<NetworkStatus>
{
    public async Task HandleAsync(NetworkStatus message, CancellationToken token)
    {
        var currentConnection = routerService.GetCurrentConnection();
        if (!string.IsNullOrEmpty(message.UdpDest) && message.UdpDest != currentConnection.Name)
        {
            var nextHop = routerService.GetNextHop(message.UdpDest);
            if (nextHop == null)
            {
                logger.LogError("Destination {Destination} is not reachable from this drone.", message.UdpDest);
                return;
            }

            await udpService.SendMessageAsync(message, nextHop.IpAddress, nextHop.UdpPort);
        }
        
        var previousConnection = routerService.GetConnectionOrNull(Connection.GetName(message.Id, (ConnectionType) message.Type));
        if (previousConnection == null)
        {
            return;
        }

        var connection = previousConnection with
        {
            IpAddress = message.IpAddress,
            Http1Port = message.Http1Port,
            GrpcPort = message.GrpcPort,
            UdpPort = message.UdpPort,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
        
        _ = routerService.TryUpdateConnection(connection, message.AliveConnectionNames.ToHashSet());
    }
}
