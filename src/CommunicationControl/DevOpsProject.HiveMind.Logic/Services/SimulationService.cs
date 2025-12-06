using DevOpsProject.HiveMind.Logic.Services.Interfaces;

namespace DevOpsProject.HiveMind.Logic.Services;

public sealed class SimulationService : ISimulationService
{
    public ISet<string> IgnoredConnectionNames {get;} = new HashSet<string>();
    
    public bool AddIgnoredConnection(string connectionName)
    {
        return IgnoredConnectionNames.Add(connectionName);
    }

    public bool RemoveIgnoredConnection(string connectionName)
    {
        return IgnoredConnectionNames.Remove(connectionName);
    }

    public bool IsIgnoredConnection(string connectionName)
    {
        return IgnoredConnectionNames.Contains(connectionName);
    }
}