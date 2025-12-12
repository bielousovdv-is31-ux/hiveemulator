namespace DevOpsProject.Shared.Simulation;

public interface ISimulationUtility
{
    TimeSpan? BadDeviceLatency { get; }
    void SimulateBadDevice(BadDevice badDevice);
    void StopBadDeviceSimulation();
    bool StopBadConnectionSimulation(string connectionName);
    TimeSpan? GetBadConnectionLatency(string connectionName);
    void SimulateBadConnection(BadConnection badConnection);
}
