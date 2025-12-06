using Common;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using ConnectionType = DevOpsProject.Shared.Grpc.ConnectionType;

namespace DevOpsProject.HiveMind.API;

public sealed class NetworkStatusPublisher(ILogger<NetworkStatusPublisher> logger, IOptions<HiveCommunicationConfig> communicationConfigurationOptions, IUdpService udpService, IRouterService routerService, IOptions<NetworkStatusPublisherOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting NetworkStatusPublisher");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(options.Value.Delay, stoppingToken);

                var connection = routerService.GetConnectionOrNull(Connection.GetName(communicationConfigurationOptions.Value.HiveID, Shared.Enums.ConnectionType.Hive))
                                 ?? throw new InvalidOperationException("Hive connection does not exist");
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
                    .Where(c => c.State == ConnectionState.Alive)
                    .Select(c => c.Name)
                    .ToList());
                var tasks = routerService.GetConnections()
                    .Where(c => c.Name != connection.Name)
                    .Select(c =>
                    {
                        var nextHop = routerService.GetNextHop(c.Name);
                        return udpService.SendMessageAsync(message, nextHop.IpAddress, nextHop.UdpPort);
                    });
                
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException operationCanceledException) when (
                operationCanceledException.CancellationToken == stoppingToken || stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error in NetworkStatusPublisher");
            }
        }
        
        logger.LogInformation("Stopping NetworkStatusPublisher");
    }
}
