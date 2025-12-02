using DevOpsProject.Drone.Logic.Services.Interfaces;
using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ConnectionType = DevOpsProject.Shared.Enums.ConnectionType;
using Location = DevOpsProject.Shared.Models.Location;

namespace DevOpsProject.Drone.API.Services;

public sealed class DroneGrpcService(IRouterService routerService, IDroneState droneState, IDroneService droneService) : DroneService.DroneServiceBase
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
        
        routerService.AddOrUpdateConnection(
            new Connection(request.Id, ConnectionType.Hive, request.IpAddress, request.Http1Port, request.GrpcPort, request.UdpPort, request.Timestamp.ToDateTimeOffset()),
            request.AliveConnectionNames);
        foreach (var drone in request.Drones)
        {
            routerService.AddOrUpdateConnection(
                new Connection(drone.Id, ConnectionType.Drone, drone.IpAddress, drone.Http1Port, drone.GrpcPort, drone.UdpPort, drone.Timestamp.ToDateTimeOffset()),
                drone.AliveConnectionNames);
        }
        return Task.FromResult(new ConnectHiveResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
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
        routerService.AddOrUpdateConnection(
            new Connection(request.Id, ConnectionType.Drone, request.IpAddress, request.Http1Port, request.GrpcPort, request.UdpPort, request.Timestamp.ToDateTimeOffset()),
            request.AliveConnectionNames);
        return Task.FromResult(new ConnectDroneResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
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
        var currentState = (IDroneState)droneState.Clone();
        
        return Task.FromResult(new PingResponse()
        {
            Id = droneState.DroneId,
            Type = Shared.Grpc.ConnectionType.Drone,
            IpAddress = connection.IpAddress,
            GrpcPort = connection.GrpcPort,
            Http1Port = connection.Http1Port,
            UdpPort = connection.UdpPort,
            Timestamp = DateTimeOffset.UtcNow.ToTimestamp(),
            DroneType = (DevOpsProject.Shared.Grpc.DroneType) currentState.Type,
            State = (DevOpsProject.Shared.Grpc.DroneState) currentState.State,
            Location = new DevOpsProject.Shared.Grpc.Location()
            {
                Latitude = currentState.Location.Latitude,
                Longitude = currentState.Location.Longitude,
            },
            Speed = currentState.Speed,
            Height = currentState.Height,
            Destination = currentState.Destination != null 
                ? new DevOpsProject.Shared.Grpc.Location()
                {
                    Latitude = currentState.Destination.Value.Latitude,
                    Longitude = currentState.Destination.Value.Longitude,
                }
                : null
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

    public override Task<StopDeadConnectionSimulationResponse> StopDeadConnectionSimulation(StopDeadConnectionSimulationRequest request, ServerCallContext context)
    {
        var connection = routerService.GetConnectionOrNull(request.ConnectionName);
        if (connection == null)
        {
            return Task.FromResult(new StopDeadConnectionSimulationResponse()
            {
                Result = new Result()
                {
                    Error = $"Drone connection '{droneState.Name}' does not exist"
                }
            });
        }

        if (connection.State != ConnectionState.DeadNonRecoverable)
        {
            return Task.FromResult(new StopDeadConnectionSimulationResponse()
            {
                Result = new Result()
                {
                    Error = $"Drone connection '{droneState.Name}' does not undergo any simulations."
                }
            });
        }

        connection.State = ConnectionState.Dead;

        return Task.FromResult(new StopDeadConnectionSimulationResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
            }
        });
    }

    public override Task<MoveResponse> Move(MoveRequest request, ServerCallContext context)
    {
        droneService.StartMoving(new Location()
        {
            Latitude = request.Destination.Latitude,
            Longitude = request.Destination.Longitude
        });

        return Task.FromResult(new MoveResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
            }
        });
    }

    public override Task<StopResponse> Stop(StopRequest request, ServerCallContext context)
    {
        droneService.StopMoving();
        
        return Task.FromResult(new StopResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
            }
        });
    }
}
