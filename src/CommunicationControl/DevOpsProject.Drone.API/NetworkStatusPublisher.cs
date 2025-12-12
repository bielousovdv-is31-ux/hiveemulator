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
        var connection = routerService.GetConnectionOrNull(droneState.Name)
                         ?? throw new InvalidOperationException($"Drone connection '{droneState.Name}' does not exist");

        var tasks = routerService.GetConnections()
            .Select(async c =>
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
                message.Connections.AddRange(routerService
                    .GetConnections()
                    .Select(conn => new ForeignConnection()
                    {
                        Name = conn.Name,
                        LastUpdatedAt = conn.LastUpdatedAt.ToTimestamp(),
                    })
                    .ToList());
                
                var simulationLatency = simulationUtility.BadDeviceLatency;
                if (simulationLatency.HasValue)
                {
                    await Task.Delay(simulationLatency.Value);
                }
                        
                var connectionSimulationLatency = simulationUtility.GetBadConnectionLatency(c.Name);
                if (connectionSimulationLatency.HasValue)
                {
                    await Task.Delay(connectionSimulationLatency.Value);
                }
                
                await udpService.SendMessageAsync(message, c.IpAddress, c.UdpPort);
            });
                
        await Task.WhenAll(tasks);
    }
}
