using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DevOpsProject.HiveMind.API.DronesTelemetryLogging;

public sealed class DronesTelemetryLogger(IDroneTelemetryService droneTelemetryService, IOptions<DronesTelemetryLoggerOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(options.Value.Timeout, stoppingToken);
            droneTelemetryService.LogTelemetry();
        }
    }
}