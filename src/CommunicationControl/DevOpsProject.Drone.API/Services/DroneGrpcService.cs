using DevOpsProject.Drone.Logic.Services.Interfaces;
using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using DevOpsProject.Shared.Simulation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ConnectionType = DevOpsProject.Shared.Enums.ConnectionType;
using ForeignConnection = DevOpsProject.Shared.Models.ForeignConnection;
using Location = DevOpsProject.Shared.Models.Location;

namespace DevOpsProject.Drone.API.Services;

public sealed class DroneGrpcService(
    IRouterService routerService, 
    IDroneState droneState, 
    IDroneService droneService,
    ISimulationUtility simulationUtility) : DroneService.DroneServiceBase
{
    public override Task<ConnectHiveResponse> ConnectHive(ConnectHiveRequest request, ServerCallContext context)
    {
        var hiveMindConnection = routerService.GetHiveMindConnection();
        if (hiveMindConnection != null && hiveMindConnection.Name != Connection.GetName(request.Id, ConnectionType.Hive))
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
            request.Connections.Select(c => new ForeignConnection(c.Name, c.LastUpdatedAt.ToDateTimeOffset())));
        foreach (var drone in request.Drones)
        {
            routerService.AddOrUpdateConnection(
                new Connection(drone.Id, ConnectionType.Drone, drone.IpAddress, drone.Http1Port, drone.GrpcPort, drone.UdpPort, drone.Timestamp.ToDateTimeOffset()),
                drone.Connections.Select(c => new ForeignConnection(c.Name, c.LastUpdatedAt.ToDateTimeOffset())));
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
        
        var isRemoved = routerService.TryRemoveConnection(Connection.GetName(request.Id, ConnectionType.Hive));
        foreach (var droneId in request.DroneIds)
        {
            _ =  routerService.TryRemoveConnection(Connection.GetName(droneId, ConnectionType.Drone));
        }
        return Task.FromResult(new DisconnectHiveResponse()
        {
            Result = new Result()
            {
                IsSuccess = isRemoved
            }
        });
    }

    public override Task<ConnectDroneResponse> ConnectDrone(ConnectDroneRequest request, ServerCallContext context)
    {
        routerService.AddOrUpdateConnection(
            new Connection(request.Id, ConnectionType.Drone, request.IpAddress, request.Http1Port, request.GrpcPort, request.UdpPort, request.Timestamp.ToDateTimeOffset()),
            request.Connections.Select(c => new ForeignConnection(c.Name, c.LastUpdatedAt.ToDateTimeOffset())));
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
        _ = routerService.TryRemoveConnection(Connection.GetName(request.Id, ConnectionType.Drone));
        return Task.FromResult(new DisconnectDroneResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
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

    public override Task<SimulateBadConnectionResponse> SimulateBadConnection(SimulateBadConnectionRequest request, ServerCallContext context)
    {
        var connection = routerService.GetConnectionOrNull(request.ConnectionName);
        if (connection == null)
        {
            return Task.FromResult(new SimulateBadConnectionResponse()
            {
                Result = new Result()
                {
                    Error = $"Drone connection '{droneState.Name}' does not exist"
                }
            });
        }
        
        simulationUtility.SimulateBadConnection(new BadConnection(request.ConnectionName, request.Latency.ToTimeSpan(), request.Duration?.ToTimeSpan()));

        return Task.FromResult(new SimulateBadConnectionResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
            }
        });
    }

    public override Task<StopBadConnectionSimulationResponse> StopBadConnectionSimulation(StopBadConnectionSimulationRequest request, ServerCallContext context)
    {
        var connection = routerService.GetConnectionOrNull(request.ConnectionName);
        if (connection == null)
        {
            return Task.FromResult(new StopBadConnectionSimulationResponse()
            {
                Result = new Result()
                {
                    Error = $"Drone connection '{droneState.Name}' does not exist"
                }
            });
        }
        
        var result = simulationUtility.StopBadConnectionSimulation(request.ConnectionName);

        return Task.FromResult(new StopBadConnectionSimulationResponse()
        {
            Result = new Result()
            {
                IsSuccess = result,
                Error = result ? null : $"Drone connection '{droneState.Name}' does not undergo any simulations."
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

    public override Task<SimulateBadDeviceResponse> SimulateBadDevice(SimulateBadDeviceRequest request, ServerCallContext context)
    {
        simulationUtility.SimulateBadDevice(new BadDevice(request.Latency.ToTimeSpan(), request.Duration?.ToTimeSpan()));

        return Task.FromResult(new SimulateBadDeviceResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
            }
        });
    }

    public override Task<StopBadDeviceSimulationResponse> StopBadDeviceSimulation(StopBadDeviceSimulationRequest request, ServerCallContext context)
    {
        simulationUtility.StopBadDeviceSimulation();

        return Task.FromResult(new StopBadDeviceSimulationResponse()
        {
            Result = new Result()
            {
                IsSuccess = true
            }
        });
    }
}
