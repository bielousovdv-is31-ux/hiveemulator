using Common;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Routing;
using Listener;

namespace DevOpsProject.Drone.API;

public sealed class DroneTelemetryReroutingHandler(IRouterService routerService, IUdpService udpService, ILogger<DroneTelemetryReroutingHandler> logger) : IUdpMessageHandler<DroneTelemetry>
{
    public async Task HandleAsync(DroneTelemetry message, CancellationToken token)
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

            logger.LogInformation("Redirecting drone telemetry message {Message} to {Destination}", message, nextHop.Name);
            await udpService.SendMessageAsync(message, nextHop.IpAddress, nextHop.UdpPort);
        }
    }
}
