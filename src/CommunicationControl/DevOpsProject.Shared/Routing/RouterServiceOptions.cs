using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Routing;

public class RouterServiceOptions
{
    [Required]
    public TimeSpan RouterUpdatedDelay { get; set; }
    [Required]
    public TimeSpan IsAliveCheckerDelay { get; set; }
    [Required]
    public TimeSpan IsAliveCheckerMaxDifference { get; set; }
    [Required]
    public Func<string> CurrentConnectionNameProvider { get; set; }
}