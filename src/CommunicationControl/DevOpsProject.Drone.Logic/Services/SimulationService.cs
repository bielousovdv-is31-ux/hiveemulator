using DevOpsProject.Drone.Logic.Services.Interfaces;

namespace DevOpsProject.Drone.Logic.Services;

public sealed class SimulationService : ISimulationService
{
    public bool IsStopped {get; set;}
    public ISet<string> IgnoredConnectionNames {get;} = new HashSet<string>();
    
    public void Stop()
    {
        IsStopped = true;
    }
    
    public void Restart()
    {
        IsStopped = false;
    }
    
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
