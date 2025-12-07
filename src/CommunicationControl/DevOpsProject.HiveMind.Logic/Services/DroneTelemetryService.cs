using System.Collections.Concurrent;
using DevOpsProject.HiveMind.Logic.Models;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.HiveMind.Logic.State;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.HiveMind.Logic.Services;

public sealed class DroneTelemetryService(IRouterService routerService, ILogger<DroneTelemetryService> logger, IOptions<HiveCommunicationConfig> communicationConfigurationOptions) : IDroneTelemetryService
{
    private readonly ConcurrentDictionary<string, DroneTelemetryModel> _drones = new();

    public bool TryAdd(DroneTelemetryModel model)
    {
        return _drones.TryAdd(model.Id, model);
    }

    public DroneTelemetryModel GetTelemetryModel(string droneId)
    {
        return _drones.GetValueOrDefault(droneId);
    }

    public IReadOnlyList<DroneTelemetryModel> GetTelemetryModels()
    {
        return _drones.Values.OrderBy(d => d.Id).ToList();
    }

    public void Update(DroneTelemetryModel model)
    {
        _drones.AddOrUpdate(
            model.Id,
            _ => throw new KeyNotFoundException($"Drone {model.Id} not found in ConcurrentDictionary."),
            (_, _) => model
        );
    }

    public void UpdateHiveMindLocation()
    {
        var values = _drones.Values
            .Where(c => routerService.GetConnectionOrNull(Connection.GetName(c.Id, ConnectionType.Drone))?.State == ConnectionState.Alive)
            .ToList();
        if (values.Count == 0)
        {
            return;
        }
        
        var averageLatitude = values.Where(x => x.Location != null).Average(x => x.Location.Value.Latitude);
        var averageLongitude = values.Where(x => x.Location != null).Average(x => x.Location.Value.Longitude);
        HiveInMemoryState.CurrentLocation = new Location()
        {
            Latitude = averageLatitude,
            Longitude = averageLongitude
        };
        HiveInMemoryState.IsMoving = values.Any(x => x.State == DroneState.Moving);
    }
    
    public bool TryRemove(string droneId)
    {
        return _drones.TryRemove(droneId, out _);
    }

    public void LogTelemetry()
    {
        var currentTime = DateTimeOffset.UtcNow;

        var hiveMindConnections = routerService.GetConnectedDevicesNames(Connection.GetName(communicationConfigurationOptions.Value.HiveID, ConnectionType.Hive));
        logger.LogInformation("[{Timestamp}] HiveMind {Id}: {State} Location: ({LocationLat:F6},{LocationLon:F6}) Destination: ({DestinationLat:F6},{DestinationLon:F6})", 
            currentTime, 
            communicationConfigurationOptions.Value.HiveID,
            HiveInMemoryState.IsMoving ? "Moving" : "Static",
            HiveInMemoryState.CurrentLocation?.Latitude,
            HiveInMemoryState.CurrentLocation?.Longitude,
            HiveInMemoryState.Destination?.Latitude,
            HiveInMemoryState.Destination?.Longitude);
        logger.LogInformation("[{Timestamp}] HiveMind {Id} Connections: {ConnectionsNames}", 
            currentTime, 
            communicationConfigurationOptions.Value.HiveID,
            string.Join(", ", hiveMindConnections));
        
        var drones = _drones.Values.OrderBy(d => d.Id).ToList();
        foreach (var drone in drones)
        {
            var connection = routerService.GetConnectionOrNull(Connection.GetName(drone.Id, ConnectionType.Drone));
            var connectedDevices =
                routerService.GetConnectedDevicesNames(Connection.GetName(drone.Id, ConnectionType.Drone));
            if (connection is null)
            {
                logger.LogWarning("[{Timestamp}] No connection found for {DroneId}.", currentTime, drone.Id);
                continue;
            }
            
            logger.LogInformation("[{Timestamp}] Drone {DroneId}, {DroneType}: {ConnectionStatus} {State} Location: ({LocationLat:F6},{LocationLon:F6}) Destination: ({DestinationLat:F6},{DestinationLon:F6}) Last updated at: {LastUpdatedAt}, Conn last updated at: {ConnectionLastUpdatedAt}", 
                currentTime, 
                drone.Id,
                drone.DroneType,
                connection.State,
                drone.State,
                drone.Location?.Latitude,
                drone.Location?.Longitude,
                drone.Destination?.Latitude,
                drone.Destination?.Longitude,
                drone.LastUpdatedAt,
                connection.LastUpdatedAt);
            logger.LogInformation("[{TimeStamp}] Drone {DroneId}: Connections: {ConnectionsNames}", currentTime, drone.Id, string.Join(", ", connectedDevices));
        }
    }
}
