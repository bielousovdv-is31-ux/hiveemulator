namespace DevOpsProject.HiveMind.Logic.Services.Interfaces;

public interface ISimulationService
{
    bool AddIgnoredConnection(string connectionName);
    bool RemoveIgnoredConnection(string connectionName);
    bool IsIgnoredConnection(string connectionName);
}