using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Routing;

public interface IRouterService
{
    Connection GetNextHop(string name);
    Connection GetConnection(string name);
    bool IsHiveMindConnected();
    ISet<string> GetConnectedDevicesNames(string name);
    IReadOnlyList<Connection> GetConnections();
    void AddConnection(Connection connection, ISet<string> connectedDevicesNames);
    void UpdateConnection(Connection connection, ISet<string> connectedDevicesNames);
    void RemoveConnection(Connection connection, ISet<string> connectedDevicesNames);
    bool IsRecalculationNeeded();
    void RecalculateHops(string currentConnectionName);
}
