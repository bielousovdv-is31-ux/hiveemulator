namespace DevOpsProject.Shared.Simulation;

public interface ISimulationUtility
{
    bool IsStopped { get; }
    void Stop(TimeSpan? duration = null);
    void Restart();
    bool RemoveIgnoredConnection(string connectionName);
    bool IsIgnoredConnection(string connectionName);
    bool AddIgnoredConnection(string connectionName, TimeSpan? duration = null);
}
