using System.Collections.Concurrent;

namespace DevOpsProject.Shared.Simulation;

public sealed class SimulationUtility : ISimulationUtility
{
    public TimeSpan? BadDeviceLatency => _badDevice is { IsActive: true }
        ? _badDevice.Latency
        : null;
    private BadDevice _badDevice;
    
    private readonly ConcurrentDictionary<string, BadConnection> _connections = new();
    
    public void SimulateBadDevice(BadDevice badDevice)
    {
        _badDevice = badDevice;
    }

    public void StopBadDeviceSimulation()
    {
        _badDevice = null;
    }

    public bool StopBadConnectionSimulation(string connectionName)
    {
        return _connections.TryRemove(connectionName, out _);
    }

    public TimeSpan? GetBadConnectionLatency(string connectionName)
    {
        var value = _connections.GetValueOrDefault(connectionName);
        
        return value is { IsActive: true } ? value.Latency : null;
    }

    public void SimulateBadConnection(BadConnection badConnection)
    {
        _connections[badConnection.Name] = badConnection;
    }
}
