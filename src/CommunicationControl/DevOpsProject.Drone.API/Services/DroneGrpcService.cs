using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ConnectionType = DevOpsProject.Shared.Enums.ConnectionType;

namespace DevOpsProject.Drone.API.Services;

public sealed class DroneGrpcService(IRouterService routerService, IDroneState droneState) : DroneService.DroneServiceBase
{
    public override Task<ConnectHiveResponse> ConnectHive(ConnectHiveRequest request, ServerCallContext context)
    {
        if (routerService.IsHiveMindConnected())
        {
            return Task.FromResult(new ConnectHiveResponse()
            {
                Result = new Result()
                {
                    IsSuccess = false,
                    Error = "Hive is already connected"
                }
            });
        }
        
        var added = routerService.TryAddConnection(
            new Connection(request.Id, ConnectionType.Hive, request.IpAddress, request.Http1Port, request.GrpcPort, request.UdpPort, request.Timestamp.ToDateTimeOffset()),
            request.AliveConnectionNames);
        return Task.FromResult(new ConnectHiveResponse()
        {
            Result = new Result()
            {
                IsSuccess = added
            }
        });
    }

    public override Task<DisconnectHiveResponse> DisconnectHive(DisconnectHiveRequest request, ServerCallContext context)
    {
        if (!routerService.IsHiveMindConnected())
        {
            return Task.FromResult(new DisconnectHiveResponse()
            {
                Result = new Result()
                {
                    IsSuccess = false,
                    Error = "Hive is not connected"
                }
            });
        }
        
        var removed = routerService.TryRemoveConnection(Connection.GetName(request.Id, ConnectionType.Hive));
        return Task.FromResult(new DisconnectHiveResponse()
        {
            Result = new Result()
            {
                IsSuccess = removed
            }
        });
    }

    public override Task<ConnectDroneResponse> ConnectDrone(ConnectDroneRequest request, ServerCallContext context)
    {
        var added = routerService.TryAddConnection(
            new Connection(request.Id, ConnectionType.Drone, request.IpAddress, request.Http1Port, request.GrpcPort, request.UdpPort, request.Timestamp.ToDateTimeOffset()),
            request.AliveConnectionNames);
        return Task.FromResult(new ConnectDroneResponse()
        {
            Result = new Result()
            {
                IsSuccess = added,
                Error = added ? null : "This drone is already connected"
            }
        });
    }

    public override Task<DisconnectDroneResponse> DisconnectDrone(DisconnectDroneRequest request,
        ServerCallContext context)
    {
        var removed = routerService.TryRemoveConnection(Connection.GetName(request.Id, ConnectionType.Drone));
        return Task.FromResult(new DisconnectDroneResponse()
        {
            Result = new Result()
            {
                IsSuccess = removed,
                Error = removed ? null : "This drone is already connected"
            }
        });
    }

    public override Task<PingResponse> Ping(PingRequest request, ServerCallContext context)
    {
        var connection = routerService.GetConnectionOrNull(droneState.Name)
                         ?? throw new InvalidOperationException($"Drone connection '{droneState.Name}' does not exist");
        
        return Task.FromResult(new PingResponse()
        {
            Id = droneState.DroneId,
            Type = Shared.Grpc.ConnectionType.Drone,
            IpAddress = connection.IpAddress,
            GrpcPort = connection.GrpcPort,
            Http1Port = connection.Http1Port,
            UdpPort = connection.UdpPort,
            Timestamp = DateTimeOffset.UtcNow.ToTimestamp()
        });
    }

    public override Task<SimulateDeadConnectionResponse> SimulateDeadConnection(SimulateDeadConnectionRequest request, ServerCallContext context)
    {
        var connection = routerService.GetConnectionOrNull(request.ConnectionName);
        if (connection == null)
        {
            return Task.FromResult(new SimulateDeadConnectionResponse()
            {
                Result = new Result()
                {
                    Error = $"Drone connection '{droneState.Name}' does not exist"
                }
            });
        }

        connection.State = ConnectionState.DeadNonRecoverable;

        return Task.FromResult(new SimulateDeadConnectionResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
            }
        });
    }
}
