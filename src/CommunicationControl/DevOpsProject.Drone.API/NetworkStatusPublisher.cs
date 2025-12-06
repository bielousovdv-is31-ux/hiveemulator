using Common;
using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Routing;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using ConnectionType = DevOpsProject.Shared.Grpc.ConnectionType;

namespace DevOpsProject.Drone.API;

public sealed class NetworkStatusPublisher(ILogger<NetworkStatusPublisher> logger, IUdpService udpService, IRouterService routerService, IDroneState droneState, IOptions<NetworkStatusPublisherOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting NetworkStatusPublisher");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(options.Value.Delay, stoppingToken);

                var connection = routerService.GetConnectionOrNull(droneState.Name)
                                 ?? throw new InvalidOperationException($"Drone connection '{droneState.Name}' does not exist");

                var tasks = routerService.GetConnections()
                    .Select(c =>
                    {
                        var nextHop = routerService.GetNextHop(c.Name);
                        if (nextHop == null)
                        {
                            logger.LogWarning("{Name} is currently unreachable", c.Name);
                            return Task.CompletedTask;
                        }
                        
                        var message = new NetworkStatus()
                        {
                            Id = droneState.DroneId,
                            Type = ConnectionType.Drone,
                            IpAddress = connection.IpAddress,
                            Http1Port = connection.Http1Port,
                            GrpcPort = connection.GrpcPort,
                            UdpPort = connection.UdpPort,
                            SentAt = DateTimeOffset.UtcNow.ToTimestamp(),
                            UdpDest = c.Name
                        };
                        message.AliveConnectionNames.AddRange(routerService
                            .GetConnections()
                            .Where(conn => conn.State == ConnectionState.Alive)
                            .Select(conn => conn.Name)
                            .ToList());
                        
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
