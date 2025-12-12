namespace DevOpsProject.Shared.Simulation;

public abstract record BadObjectBase(TimeSpan Latency, TimeSpan? Duration)
{
    public DateTimeOffset? StopTime { get; } = Duration.HasValue ? DateTimeOffset.UtcNow.Add(Duration.Value) : null;
    public bool IsActive => !Duration.HasValue || StopTime >= DateTimeOffset.UtcNow;
}
