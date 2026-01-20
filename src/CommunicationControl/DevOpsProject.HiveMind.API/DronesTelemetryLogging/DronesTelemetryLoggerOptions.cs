using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.HiveMind.API.DronesTelemetryLogging;

public sealed class DronesTelemetryLoggerOptions
{
    [Required]
    public TimeSpan Timeout { get; set; }
}
