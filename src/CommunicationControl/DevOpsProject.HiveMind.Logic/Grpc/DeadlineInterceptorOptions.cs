using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.HiveMind.Logic.Grpc;

public sealed class DeadlineInterceptorOptions
{
    [Required]
    public TimeSpan Deadline {get; set;}
}
