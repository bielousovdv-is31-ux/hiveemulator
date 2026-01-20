using Common;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using DevOpsProject.Shared.Simulation;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using ConnectionType = DevOpsProject.Shared.Grpc.ConnectionType;

namespace DevOpsProject.HiveMind.API;

public sealed class NetworkStatusPublisher(ILogger<NetworkStatusPublisher> logger, IOptions<HiveCommunicationConfig> communicationConfigurationOptions, IUdpService udpService, IRouterService routerService, IOptions<NetworkStatusPublisherOptions> options, ISimulationUtility simulationUtility) : NetworkStatusPublisherBase(logger, options)
{
    protected override async Task PublishStatusAsync()
    {
        var connection = routerService.GetConnectionOrNull(Connection.GetName(communicationConfigurationOptions.Value.HiveID, Shared.Enums.ConnectionType.Hive))
                         ?? throw new InvalidOperationException("Hive connection does not exist");

        var tasks = routerService.GetConnections()
            .Where(c => !simulationUtility.IsIgnoredConnection(c.Name))
            .Select(c =>
            {
                var message = new NetworkStatus()
                {
                    Id = communicationConfigurationOptions.Value.HiveID,
                    Type = ConnectionType.HiveMind,
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
