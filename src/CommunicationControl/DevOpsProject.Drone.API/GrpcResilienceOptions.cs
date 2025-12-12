using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Drone.API;

public sealed class GrpcResilienceOptions
{
    [Range(1, int.MaxValue)]
    public int MaxRetryAttempts {get; set;}
    [Required]
    public TimeSpan InitialDelay {get; set;}
    [Required]
    public TimeSpan MaxDeadline {get; set;}
}