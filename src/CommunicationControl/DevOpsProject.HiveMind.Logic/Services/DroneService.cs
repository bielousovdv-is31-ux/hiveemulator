using DevOpsProject.HiveMind.Logic.Exceptions;
using DevOpsProject.HiveMind.Logic.Grpc;
using DevOpsProject.HiveMind.Logic.Models;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.HiveMindCommands;
using DevOpsProject.Shared.Routing;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
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
    IOptions<HiveCommunicationConfig> communicationConfigurationOptions,
    ILogger<DroneService> logger,
    ISimulationService simulationService) : IDroneService
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
        catch (Exception ex)
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
        _ = droneTelemetryService.TryAdd(new DroneTelemetryModel(
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
        var hiveConnection = routerService.GetHiveMindConnection();
        var connectionsToSend = connections
            .Where(c => c.Name != connection.Name)
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
                request.AliveConnectionNames.AddRange(routerService.GetConnectedDevicesNames(c.Name));
                return request;
            })
            .ToList();

        await ConnectHiveToDroneAsync(hiveConnection, connectDronesRequests, channel);
        await ConnectDroneToDronesAsync(connectionsToSend, pingResponse);
    }

    private async Task ConnectHiveToDroneAsync(Connection hiveConnection, List<ConnectDroneRequest> connectDronesRequests, GrpcChannel channel)
    {
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
        request.Drones.AddRange(connectDronesRequests);
        
        var callInvoker = channel.Intercept(retryInterceptor, logHandleExceptionInterceptor);
        var client = new Shared.Grpc.DroneService.DroneServiceClient(callInvoker);
        var connectionResult = await client.ConnectHiveAsync(request);
        if (!connectionResult.Result.IsSuccess)
        {
            throw new DroneRequestFailedException(connectionResult.Result.Error);
        }
    }
    
    private async Task ConnectDroneToDronesAsync(IEnumerable<Connection> droneConnections, PingResponse pingResponse)
    {
        var tasks = droneConnections
            .Select(async droneConnection =>
            {
                var nextHop = routerService.GetNextHop(droneConnection.Name);
                if (nextHop == null)
                {
                    logger.LogError("Drone '{DroneConnectionName}' is unreachable.", droneConnection.Name);
                    return;
                }
                
                var connectionChannel = grpcChannelFactory.Create(nextHop.GrpcUri);
                var connectionCallInvoker = connectionChannel.Intercept(retryInterceptor, logHandleExceptionInterceptor);
                var connectionClient = new Shared.Grpc.DroneService.DroneServiceClient(connectionCallInvoker);
                var request = new ConnectDroneRequest()
                {
                    Id = pingResponse.Id,
                    IpAddress = pingResponse.IpAddress,
                    Http1Port = pingResponse.Http1Port,
                    GrpcPort = pingResponse.GrpcPort,
                    UdpPort = pingResponse.UdpPort,
                    Timestamp = DateTimeOffset.UtcNow.ToTimestamp()
                };
                var result = await connectionClient.ConnectDroneAsync(
                    request,
                    headers: new Metadata()
                    {
                        {RoutingConstants.DestinationHeaderName, droneConnection.Name},
                    });
                if (!result.Result.IsSuccess)
                {
                    logger.LogError("Drone '{DroneConnectionName}' failed to connect.", droneConnection.Name);
                }
            });
        await Task.WhenAll(tasks);
    }

    public async Task DisconnectDroneAsync(string droneId)
    {
        var droneConnection = routerService.GetConnectionOrNull(Connection.GetName(droneId, ConnectionType.Drone));
        if (droneConnection == null)
        {
            throw new DroneRequestFailedException("Drone not found.");
        }
        
        var connections = routerService.GetConnections();
        var connectionsToSend = connections
            .Where(c => c.Name != droneConnection.Name)
            .ToList();
        
        await DisconnectHiveFromDroneAsync(droneConnection, connectionsToSend.Select(c => c.DeviceId));
        await DisconnectDroneFromDronesAsync(droneId, connectionsToSend);
        
        _ = routerService.TryRemoveConnection(Connection.GetName(droneId, ConnectionType.Drone));
        _ = droneTelemetryService.TryRemove(droneId);
    }

    private async Task DisconnectHiveFromDroneAsync(Connection droneConnection, IEnumerable<string> connectedDronesIds)
    {
        var nextHop = routerService.GetNextHop(droneConnection.Name);
        if (nextHop == null)
        {
            throw new DroneRequestFailedException("Drone is currently unreachable.");
        }
        
        var channel = grpcChannelFactory.Create(nextHop.GrpcUri);
        var callInvoker = channel.Intercept(retryInterceptor, logHandleExceptionInterceptor);
        var client = new Shared.Grpc.DroneService.DroneServiceClient(callInvoker);

        var request = new DisconnectHiveRequest()
        {
            Id = communicationConfigurationOptions.Value.HiveID
        };
        request.DroneIds.AddRange(connectedDronesIds);
        var result = await client.DisconnectHiveAsync(request,
            headers: new Metadata()
            {
                {RoutingConstants.DestinationHeaderName, droneConnection.Name},
            });
        if (!result.Result.IsSuccess)
        {
            throw new DroneRequestFailedException($"Drone '{droneConnection.Name}' failed to disconnect.");
        }
    }

    private async Task DisconnectDroneFromDronesAsync(string droneId, IEnumerable<Connection> connections)
    {
        var tasks = connections
            .Select(async c =>
            {
                var nextHop = routerService.GetNextHop(c.Name);
                if (nextHop == null)
                {
                    logger.LogError("Drone '{DroneConnectionName}' is unreachable.", c.Name);
                    return;
                }
                
                var connectionChannel = grpcChannelFactory.Create(nextHop.GrpcUri);
                var connectionCallInvoker = connectionChannel.Intercept(retryInterceptor, logHandleExceptionInterceptor);
                var connectionClient = new Shared.Grpc.DroneService.DroneServiceClient(connectionCallInvoker);
                var request = new DisconnectDroneRequest()
                {
                    Id = droneId,
                };
                var result = await connectionClient.DisconnectDroneAsync(
                    request,
                    headers: new Metadata()
                    {
                        {RoutingConstants.DestinationHeaderName, c.Name},
                    });
                if (!result.Result.IsSuccess)
                {
                    logger.LogError("Drone '{DroneConnectionName}' failed to disconnect the requested drone.", c.Name);
                }
            });
        await Task.WhenAll(tasks);
    }
    
    public async Task SimulateDeadConnectionAsync(SimulateDeadConnectionCommand command)
    {
        var (connection1, connection2) = (routerService.GetConnectionOrNull(command.Connection1Name), routerService.GetConnectionOrNull(command.Connection2Name));

        if (connection1 == null || connection2 == null)
        {
            throw new DroneRequestFailedException("One of connections was not found.");
        }

        if (command.Connection1Name == command.Connection2Name)
        {
            throw new DroneRequestFailedException("Connection1 and Connection2 could not be the same nodes.");
        }

        var currentConnection = routerService.GetCurrentConnection();

        var nextHop1 = routerService.GetNextHop(command.Connection1Name);
        var nextHop2 = routerService.GetNextHop(command.Connection2Name);
        var connection1IsReachable = nextHop1 != null || command.Connection1Name == currentConnection.Name;
        var connection2IsReachable = nextHop2 != null || command.Connection2Name == currentConnection.Name;
        if (!connection1IsReachable || !connection2IsReachable)
        {
            throw new DroneRequestFailedException("One of connections is unreachable.");
        }

        if (command.Connection1Name == currentConnection.Name)
        {
            _ = simulationService.AddIgnoredConnection(command.Connection2Name);
        }
        else
        {
            await SendSimulateDeadConnectionAsync(connection1, nextHop1, command.Connection2Name);
        }

        if (command.Connection2Name == currentConnection.Name)
        {
            _ = simulationService.AddIgnoredConnection(command.Connection1Name);
        }
        else
        {
            await SendSimulateDeadConnectionAsync(connection2, nextHop2, command.Connection1Name);
        }
    }

    private async Task SendSimulateDeadConnectionAsync(Connection sendTo, Connection nextHop, string connectionName)
    {
        var channel = grpcChannelFactory.Create(nextHop.GrpcUri);
        var callInvoker = channel.Intercept(retryInterceptor, logHandleExceptionInterceptor);
        var client = new Shared.Grpc.DroneService.DroneServiceClient(callInvoker);

        var result = await client.SimulateDeadConnectionAsync(new SimulateDeadConnectionRequest()
        {
            ConnectionName = connectionName
        }, new Metadata()
        {
            {RoutingConstants.DestinationHeaderName, sendTo.Name}
        });
        if (!result.Result.IsSuccess)
        {
            logger.LogError("Simulation start failed on connection {ConnectionName} {Result}.", sendTo.Name, result);
        }
    }

    public async Task StopDeadConnectionSimulationAsync(StopDeadConnectionSimulationCommand command)
    {
        var (connection1, connection2) = (routerService.GetConnectionOrNull(command.Connection1Name), routerService.GetConnectionOrNull(command.Connection2Name));

        if (connection1 == null || connection2 == null)
        {
            throw new DroneRequestFailedException("One of connections was not found.");
        }

        if (command.Connection1Name == command.Connection2Name)
        {
            throw new DroneRequestFailedException("Connection1 and Connection2 could not be the same nodes.");
        }
        
        var currentConnection = routerService.GetCurrentConnection();
        
        if (command.Connection1Name == currentConnection.Name)
        {
            _ = simulationService.RemoveIgnoredConnection(command.Connection2Name);
        }
        else
        {
            await SendStopDeadConnectionSimulationAsync(connection1, command.Connection2Name);
        }

        if (command.Connection2Name == currentConnection.Name)
        {
            _ = simulationService.RemoveIgnoredConnection(command.Connection1Name);
        }
        else
        {
            await SendStopDeadConnectionSimulationAsync(connection2, command.Connection1Name);
        }
    }

    private async Task SendStopDeadConnectionSimulationAsync(Connection sendTo, string connectionName)
    {
        var channel = grpcChannelFactory.Create(sendTo.GrpcUri);
        var callInvoker = channel.Intercept(retryInterceptor, logHandleExceptionInterceptor);
        var client = new Shared.Grpc.DroneService.DroneServiceClient(callInvoker);

        var result = await client.StopDeadConnectionSimulationAsync(new StopDeadConnectionSimulationRequest
        {
            ConnectionName = connectionName
        });
        if (!result.Result.IsSuccess)
        {
            logger.LogError("Simulation stop failed on connection {ConnectionName} {Result}.", sendTo.Name, result);
        }
    }
}
