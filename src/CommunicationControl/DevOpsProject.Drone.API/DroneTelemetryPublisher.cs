using Common;
using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Routing;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using DroneState = DevOpsProject.Shared.Grpc.DroneState;

namespace DevOpsProject.Drone.API;

public sealed class DroneTelemetryPublisher(ILogger<DroneTelemetryPublisher> logger, IUdpService udpService, IRouterService routerService, IDroneState droneState, IOptions<DroneTelemetryPublisherOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"Starting {nameof(DroneTelemetryPublisher)}");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(options.Value.Delay, stoppingToken);

                var currentState = (IDroneState)droneState.Clone();
                var message = new DroneTelemetry()
                {
                    Id = currentState.DroneId,
                    DroneType = (DroneType) currentState.Type,
                    State = (DroneState) currentState.State,
                    Location = new Location()
                    {
                        Latitude = currentState.Location.Latitude,
                        Longitude = currentState.Location.Longitude,
                    },
                    Speed = currentState.Speed,
                    Height = currentState.Height,
                    Timestamp = DateTimeOffset.UtcNow.ToTimestamp(),
                    Destination = currentState.Destination != null 
                    ? new Location()
                    {
                        Latitude = currentState.Destination.Value.Latitude,
                        Longitude = currentState.Destination.Value.Longitude,
                    }
                    : null
                };
                var hiveMindConnection = routerService.GetHiveMindConnection();
                if (hiveMindConnection == null)
                {
                    continue;
                }

                var nextHop = routerService.GetNextHop(hiveMindConnection.Name);
                await udpService.SendMessageAsync(message, nextHop.IpAddress, nextHop.UdpPort);
            }
            catch (OperationCanceledException operationCanceledException) when (
                operationCanceledException.CancellationToken == stoppingToken || stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error in {nameof(DroneTelemetryPublisher)}");
            }
        }
        
        logger.LogInformation($"Stopping {nameof(DroneTelemetryPublisher)}");
    }
}
