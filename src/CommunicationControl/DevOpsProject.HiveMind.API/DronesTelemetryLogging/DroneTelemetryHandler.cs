using DevOpsProject.HiveMind.Logic.Models;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Grpc;
using Listener;
using Location = DevOpsProject.Shared.Models.Location;

namespace DevOpsProject.HiveMind.API.DronesTelemetryLogging;

public sealed class DroneTelemetryHandler(IDroneTelemetryService droneTelemetryService) : IUdpMessageHandler<DroneTelemetry>
{
    public Task HandleAsync(DroneTelemetry message, CancellationToken token)
    {
        var updatedDroneTelemetry = new DroneTelemetryModel(
            message.Id,
            new Location
            {
                Latitude = message.Location.Latitude,
                Longitude = message.Location.Longitude
            },
            message.Speed,
            message.Height,
            (DevOpsProject.Shared.Enums.DroneType)message.DroneType,
            DateTimeOffset.UtcNow,
            (DevOpsProject.Shared.Enums.DroneState)message.State,
            message.Destination != null
                ? new Location
                {
                    Latitude = message.Destination.Latitude,
                    Longitude = message.Destination.Longitude
                }
                : null
        );

        try
        {
            droneTelemetryService.Update(updatedDroneTelemetry);
            droneTelemetryService.UpdateHiveMindLocation();
        }
        catch (KeyNotFoundException)
        {
        }
        
        return Task.CompletedTask;
    }
}
