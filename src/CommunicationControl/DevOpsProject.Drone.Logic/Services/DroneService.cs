using DevOpsProject.Drone.Logic.Services.Interfaces;
using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.Drone.Logic.Services;

public sealed class DroneService(IDroneState droneState, ILogger<DroneService> logger) : IDroneService, IDisposable
{
    private Timer? _movementTimer;
    private const float _stepSize = 0.1f;
    private readonly TimeSpan _movingInterval = TimeSpan.FromSeconds(3);
    private readonly Lock _movementLock = new Lock();
    
    public void StartMoving(Location destination)
    {
        lock (_movementLock)
        {
            if (droneState.State == Shared.Enums.DroneState.Moving)
            {
                logger.LogInformation("The drone is already moving, stopping movement and changing destination.");
                StopMovingInternal();
            }

            logger.LogInformation("Starting movement. Destination: {@destination}", destination);
            
            droneState.Destination = destination;
            droneState.State = Shared.Enums.DroneState.Moving;
            _movementTimer = new Timer(Move, null, TimeSpan.Zero, _movingInterval);
        }
    }

    public void StopMoving()
    {
        lock (_movementLock)
        {
            if (droneState.State == Shared.Enums.DroneState.Static)
            {
                logger.LogInformation("The drone is already stopped.");
                return;
            }

            StopMovingInternal();
            logger.LogInformation("Stopping movement. Current location: {@currentLocation}", droneState.Location);
        }
    }
    
    private void StopMovingInternal()
    {
        _movementTimer?.Dispose();
        _movementTimer = null;
        droneState.Destination = null;
        droneState.State = Shared.Enums.DroneState.Static;
    }
    
    private void Move(object? state)
    {
        lock (_movementLock)
        {
            droneState.Location = CalculateNextPosition(_stepSize);
            logger.LogInformation("Continue moving... Current location: {@location}, Destination: {@destination}", droneState.Location, droneState.Destination!.Value);
            
            if (AreLocationsEqual(droneState.Location, droneState.Destination!.Value))
            {
                logger.LogInformation("Reached destination. Current location: {@currentLocation}, Destination: {@destination}", droneState.Location, droneState.Destination!.Value);
                StopMovingInternal();
            }
        }
    }
    
    private static bool AreLocationsEqual(Location loc1, Location loc2)
    {
        const double tolerance = 0.000001;
        return Math.Abs(loc1.Latitude - loc2.Latitude) < tolerance &&
               Math.Abs(loc1.Longitude - loc2.Longitude) < tolerance;
    }

    private Location CalculateNextPosition(float stepSize)
    {
        var newLat = droneState.Location.Latitude +
                     (droneState.Destination!.Value.Latitude - droneState.Location.Latitude) * stepSize;
        var newLon = droneState.Location.Longitude +
                     (droneState.Destination.Value.Longitude - droneState.Location.Longitude) * stepSize;
        return new Location
        {
            Latitude = newLat,
            Longitude = newLon
        };
    }
    
    public void Dispose()
    {
        _movementTimer?.Dispose();
        _movementTimer = null;
    }
}
