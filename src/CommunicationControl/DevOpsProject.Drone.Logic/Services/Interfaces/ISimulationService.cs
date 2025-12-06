namespace DevOpsProject.Drone.Logic.Services.Interfaces;

public interface ISimulationService
{
    bool IsStopped { get; }
    void Stop();
    void Restart();
    bool AddIgnoredConnection(string connectionName);
    bool RemoveIgnoredConnection(string connectionName);
    bool IsIgnoredConnection(string connectionName);
}