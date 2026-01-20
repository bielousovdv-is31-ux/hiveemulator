namespace DevOpsProject.Shared.Simulation;

public sealed class SimulationUtility : ISimulationUtility
{
    public bool IsStopped => _isStoppedForever || (_stopTime.HasValue && _stopTime >= DateTimeOffset.UtcNow);
    private bool _isStoppedForever;
    private DateTimeOffset? _stopTime;
    public IDictionary<string, DateTimeOffset?> IgnoredConnectionNames {get;} = new Dictionary<string, DateTimeOffset?>();
    
    public void Stop(TimeSpan? duration)
    {
        if (duration.HasValue)
        {
            _isStoppedForever = false;
            _stopTime = DateTimeOffset.UtcNow.Add(duration.Value);
        }
        else
        {
            _isStoppedForever = true;
            _stopTime = null;
        }
    }
    
    public void Restart()
    {
        _isStoppedForever = false;
        _stopTime = null;
    }
    
    public bool AddIgnoredConnection(string connectionName, TimeSpan? duration)
    {
        IgnoredConnectionNames[connectionName] = !duration.HasValue ? null : DateTimeOffset.UtcNow.Add(duration.Value);
        return true;
    }

    public bool RemoveIgnoredConnection(string connectionName)
    {
        return IgnoredConnectionNames.Remove(connectionName);
    }

    public bool IsIgnoredConnection(string connectionName)
    {
        var containsName = IgnoredConnectionNames.TryGetValue(connectionName, out var ignoredConnection);
        return containsName && (!ignoredConnection.HasValue || ignoredConnection.Value >= DateTimeOffset.UtcNow);
    }
}
