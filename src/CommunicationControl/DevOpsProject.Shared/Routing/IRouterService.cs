using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Routing;

public interface IRouterService
{
    Connection GetNextHop(string name);
    Connection GetConnectionOrNull(string name);
    bool IsHiveMindConnected();
    ISet<string> GetConnectedDevicesNames(string name);
    IReadOnlyList<Connection> GetConnections(bool includeCurrent = false);
    void UpdateConnectionForEach(Action<Connection> action);
    bool TryAddConnection(Connection connection, IEnumerable<string> connectedDevicesNames);
    bool TryUpdateConnection(Connection connection, IEnumerable<string> connectedDevicesNames);
    bool TryRemoveConnection(string connectionName);
    bool IsRecalculationNeeded();
    void RecalculateHops();
    Connection GetHiveMindConnection();
}
