using DevOpsProject.HiveMind.Logic.Exceptions;
using DevOpsProject.HiveMind.Logic.Grpc;
using DevOpsProject.HiveMind.Logic.Models;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConnectionType = DevOpsProject.Shared.Enums.ConnectionType;
using Location = DevOpsProject.Shared.Models.Location;

namespace DevOpsProject.HiveMind.Logic.Services;

public sealed class DroneService(
    LogHandleExceptionInterceptor logHandleExceptionInterceptor, 
    IGrpcChannelFactory grpcChannelFactory, 
    ResilienceInterceptor retryInterceptor, 
    IDroneTelemetryService droneTelemetryService, 
    IRouterService routerService, 
    IOptionsSnapshot<HiveCommunicationConfig> communicationConfigurationOptions,
    ILogger<DroneService> logger) : IDroneService
{
    public async Task ConnectDroneAsync(string ipAddress, int port)
    {
        var uriBuilder = new UriBuilder(ipAddress)
        {
            Port = port
        };
        var channel = grpcChannelFactory.Create(new Uri(uriBuilder.ToString()));
        var callInvoker = channel.Intercept(retryInterceptor);
        var client = new Shared.Grpc.DroneService.DroneServiceClient(callInvoker);

        PingResponse pingResponse;
        try
        {
            pingResponse = await client.PingAsync(new PingRequest());
        }
        catch (RpcException ex)
        {
            throw new DroneRequestFailedException("Failed to ping drone", ex);
        }
        var connection = new Connection(
            pingResponse.Id,
            (ConnectionType)pingResponse.Type,
            pingResponse.IpAddress,
            pingResponse.Http1Port,
            pingResponse.GrpcPort,
            pingResponse.UdpPort,
            pingResponse.Timestamp.ToDateTimeOffset());
        routerService.AddOrUpdateConnection(connection, []);
        droneTelemetryService.Add(new DroneTelemetryModel(
            pingResponse.Id,
            pingResponse.Location != null
                ? new Location()
                {
                    Latitude = pingResponse.Location.Latitude,
                    Longitude = pingResponse.Location.Longitude
                }
                : null!,
            pingResponse.Speed,
            pingResponse.Height,
            (DevOpsProject.Shared.Enums.DroneType)pingResponse.DroneType,
            pingResponse.Timestamp.ToDateTimeOffset(),
            (DevOpsProject.Shared.Enums.DroneState)pingResponse.State,
            pingResponse.Destination != null
                ? new Location()
                {
                    Latitude = pingResponse.Destination.Latitude,
                    Longitude = pingResponse.Destination.Longitude
                }
                : null));

        var connections = routerService.GetConnections();
        var hiveConnection = connections.First(c => c.Type == ConnectionType.Hive);
        var connectionsToSend = connections
            .Where(c => c.Type != ConnectionType.Hive && c.Name != connection.Name)
            .ToList();

        var connectDronesRequests = connectionsToSend
            .Select(c =>
            {
                var request = new ConnectDroneRequest()
                {
                    Id = c.DeviceId,
                    IpAddress = c.IpAddress,
                    Http1Port = c.Http1Port,
                    GrpcPort = c.GrpcPort,
                    UdpPort = c.UdpPort,
                    Timestamp = DateTimeOffset.UtcNow.ToTimestamp()
                };
                request.AliveConnectionNames.AddRange(routerService.GetConnectedDevicesNames(c.DeviceId));
                return (c, request);
            })
            .ToList();

        var request = new ConnectHiveRequest()
        {
            Id = communicationConfigurationOptions.Value.HiveID,
            IpAddress = hiveConnection.IpAddress,
            Http1Port = hiveConnection.Http1Port,
            GrpcPort = hiveConnection.GrpcPort,
            UdpPort = hiveConnection.UdpPort,
            Timestamp = DateTimeOffset.UtcNow.ToTimestamp(),

        };
        request.AliveConnectionNames.AddRange(routerService.GetConnectedDevicesNames(hiveConnection.Name));
        request.Drones.AddRange(connectDronesRequests.Select(r => r.request));
        callInvoker = channel.Intercept(retryInterceptor, logHandleExceptionInterceptor);
        client = new Shared.Grpc.DroneService.DroneServiceClient(callInvoker);
        var connectionResult = await client.ConnectHiveAsync(request);
        if (!connectionResult.Result.IsSuccess)
        {
            throw new DroneRequestFailedException(connectionResult.Result.Error);
        }

        var tasks = connectDronesRequests
            .Select(async item =>
            {
                var (droneConnection, droneRequest) = item;
                var nextHop = routerService.GetNextHop(droneConnection.Name);
                if (nextHop == null)
                {
                    logger.LogError("Drone '{DroneConnectionName}' has is unreachable.", droneConnection.Name);
                    return;
                }
                
                var connectionChannel = grpcChannelFactory.Create(nextHop.GrpcUri);
                var connectionCallInvoker = connectionChannel.Intercept(retryInterceptor, logHandleExceptionInterceptor);
                var connectionClient = new Shared.Grpc.DroneService.DroneServiceClient(connectionCallInvoker);
                var result = await connectionClient.ConnectDroneAsync(droneRequest,
                    headers: new Metadata()
                    {
                        {RoutingConstants.DestinationHeaderName, droneConnection.Name},
                    });
                if (!result.Result.IsSuccess)
                {
                    throw new DroneRequestFailedException(result.Result.Error);
                }
            });
        await Task.WhenAll(tasks);
    }
}
