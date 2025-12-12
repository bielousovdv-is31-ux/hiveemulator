namespace DevOpsProject.Shared.Simulation;

public sealed record BadConnection(string Name, TimeSpan Latency, TimeSpan? Duration) : BadObjectBase(Latency, Duration);
