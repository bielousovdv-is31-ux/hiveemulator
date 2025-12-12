namespace DevOpsProject.Shared.Simulation;

public sealed record BadDevice(TimeSpan Latency, TimeSpan? Duration) : BadObjectBase(Latency, Duration);
