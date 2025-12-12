using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Routing;

public interface IRouterService
{
    Connection GetNextHop(string name);
    Connection GetConnectionOrNull(string name);
    bool IsHiveMindConnected();
    ICollection<ForeignConnection> GetConnectedDevices(string name);
    IReadOnlyList<Connection> GetConnections(bool includeCurrent = false);
    void AddOrUpdateConnection(Connection connection, IEnumerable<ForeignConnection> connectedDevices);
    bool TryUpdateConnection(Connection connection, IEnumerable<ForeignConnection> connectedDevices);
    bool TryRemoveConnection(string connectionName);
    void RecalculateHops();
    Connection GetHiveMindConnection();
    Connection GetCurrentConnection();
}
