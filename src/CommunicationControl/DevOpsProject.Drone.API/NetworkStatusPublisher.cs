using Common;
using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Routing;
using DevOpsProject.Shared.Simulation;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using ConnectionType = DevOpsProject.Shared.Grpc.ConnectionType;

namespace DevOpsProject.Drone.API;

public sealed class NetworkStatusPublisher(ILogger<NetworkStatusPublisher> logger, IUdpService udpService, IRouterService routerService, IDroneState droneState, IOptions<NetworkStatusPublisherOptions> options, ISimulationUtility simulationUtility) : NetworkStatusPublisherBase(logger, options)
{
    protected override async Task PublishStatusAsync()
    {
        if (simulationUtility.IsStopped)
        {
            return;
        }

        var connection = routerService.GetConnectionOrNull(droneState.Name)
                         ?? throw new InvalidOperationException($"Drone connection '{droneState.Name}' does not exist");

        var tasks = routerService.GetConnections()
            .Where(c => !simulationUtility.IsIgnoredConnection(c.Name))
            .Select(c =>
            {
                var message = new NetworkStatus()
                {
                    Id = droneState.DroneId,
                    Type = ConnectionType.Drone,
                    IpAddress = connection.IpAddress,
                    Http1Port = connection.Http1Port,
                    GrpcPort = connection.GrpcPort,
                    UdpPort = connection.UdpPort,
                    SentAt = DateTimeOffset.UtcNow.ToTimestamp()
                };
                message.AliveConnectionNames.AddRange(routerService
                    .GetConnections()
                    .Where(conn => conn.State == ConnectionState.Alive)
                    .Select(conn => conn.Name)
                    .ToList());
                        
                return udpService.SendMessageAsync(message, c.IpAddress, c.UdpPort);
            });
                
        await Task.WhenAll(tasks);
    }
}
