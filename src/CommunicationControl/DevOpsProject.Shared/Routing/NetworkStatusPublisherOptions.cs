using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Routing;

public class NetworkStatusPublisherOptions
{
    [Required]
    public TimeSpan Delay { get; set; }
}
