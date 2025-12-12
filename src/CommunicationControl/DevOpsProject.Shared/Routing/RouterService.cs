using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;
using Microsoft.Extensions.Options;

namespace DevOpsProject.Shared.Routing;

public sealed class RouterService : IRouterService
{
    private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
    
    private Dictionary<string, List<ForeignConnection>> _lastConnectedDevicesSnapshot = new();
    private readonly Dictionary<string, List<ForeignConnection>> _connectedDevices = new();
    private readonly Dictionary<string, Connection> _connections = new();
    
    private Dictionary<string, Connection> _nextHops = new();

    private readonly RouterServiceOptions _options;

    public RouterService(IOptions<RouterServiceOptions> options)
    {
        _options = options.Value;
        AddOrUpdateConnection(options.Value.CurrentConnection, []);
    }

    public Connection GetNextHop(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        
        _rwLock.EnterReadLock();
        try
        {
            return _nextHops.GetValueOrDefault(name);
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }
    
    public Connection GetConnectionOrNull(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        
        _rwLock.EnterReadLock();
        try
        {
            return _connections.GetValueOrDefault(name);
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    public bool IsHiveMindConnected()
    {
        _rwLock.EnterReadLock();
        try
        {
            return _connections.Any(c => c.Value.Type == ConnectionType.Hive);
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    public Connection GetHiveMindConnection()
    {
        _rwLock.EnterReadLock();
        try
        {
            return _connections.FirstOrDefault(c => c.Value.Type == ConnectionType.Hive).Value;
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }
    
    public ICollection<ForeignConnection> GetConnectedDevices(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        
        _rwLock.EnterReadLock();
        try
        {
            return _connectedDevices[name];
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    public IReadOnlyList<Connection> GetConnections(bool includeCurrent)
    {
        _rwLock.EnterReadLock();
        try
        {
            if (!includeCurrent)
            {
                return _connections.Values.Where(c => c.Name != _options.CurrentConnection.Name).ToList();
            }
            
            return _connections.Values.ToList();
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }
    
    public void AddOrUpdateConnection(Connection connection, IEnumerable<ForeignConnection> connectedDevices)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(connectedDevices);
        
        _rwLock.EnterWriteLock();
        try
        {
            _connections[connection.Name] = connection;
            _connectedDevices[connection.Name] = connectedDevices.ToList();

            _nextHops[connection.Name] = connection;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
    
    public bool TryUpdateConnection(Connection connection, IEnumerable<ForeignConnection> connectedDevices)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(connectedDevices);
        
        _rwLock.EnterWriteLock();
        try
        {
            if (!_connections.ContainsKey(connection.Name))
            {
                return false;
            }
            
            _connections[connection.Name] = connection;
            _connectedDevices[connection.Name] = connectedDevices.ToList();
            
            return true;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
    
    public bool TryRemoveConnection(string connectionName)
    {
        ArgumentNullException.ThrowIfNull(connectionName);
        
        _rwLock.EnterWriteLock();
        try
        {
            var result = _connections.Remove(connectionName);
            _ = _connectedDevices.Remove(connectionName);
            if (connectionName != _options.CurrentConnection.Name)
            {
                var connectedDevice = _connectedDevices[_options.CurrentConnection.Name]
                    .FirstOrDefault(c => c.ConnectionName == connectionName);
                if (connectedDevice != null)
                {
                    _connectedDevices[_options.CurrentConnection.Name].Remove(connectedDevice);
                }
            }
            
            return result;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
    
    public void RecalculateHops()
    {
        var currentConnectionName = _options.CurrentConnection.Name;
        
        _rwLock.EnterWriteLock();

        try
        {
            _lastConnectedDevicesSnapshot = new Dictionary<string, List<ForeignConnection>>();
            foreach (var (connectionName, devices) in _connectedDevices)
            {
                _lastConnectedDevicesSnapshot[connectionName] = new List<ForeignConnection>(devices);
            }
            
            _nextHops = new Dictionary<string, Connection>();
            
            RecalculateHopsInternal();
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
    
    private void RecalculateHopsInternal()
    {
        var currentTime = DateTimeOffset.UtcNow;
        
        var connections = _connections.Values.ToList();
        var currentConnectionName = _options.CurrentConnection.Name;
        var currentConnectionCanRedirect = ConnectionCanRedirect(_options.CurrentConnection);
        
        var connectionIndexes = connections
            .Select((c, i) =>  new { c, i })
            .ToDictionary(c => c.c.Name, c => c.i);
        
        var adjacencyList = new List<Edge>[connections.Count];
        var currentConnectionIndex = connectionIndexes[currentConnectionName];
        for (var i = 0; i < connections.Count; i++)
        {
            var connection = connections[i];
            if (!ConnectionCanRedirect(connection))
            {
                continue;
            }

            var adjacentNodes = _connectedDevices[connection.Name]
                .Where(c => connectionIndexes.ContainsKey(c.ConnectionName))
                .Where(c => currentConnectionCanRedirect || c.ConnectionName != currentConnectionName)
                .Select(c => new Edge(connectionIndexes[c.ConnectionName], (currentTime - c.LastUpdatedAt).Ticks))
                .ToList();
            adjacencyList[i] = adjacentNodes;
        }
        
        var nextHops = GetNextHopsByDijkstraAlgorithm(adjacencyList, currentConnectionIndex);

        _nextHops = new Dictionary<string, Connection>();
        for (var i = 0; i < connections.Count; i++)
        {
            _nextHops[connections[i].Name] = nextHops[i].HasValue ? connections[nextHops[i].Value] : null;
        }
    }

    private static bool ConnectionCanRedirect(Connection connection) => connection.Type != ConnectionType.Hive;
    
    private static int?[] GetNextHopsByDijkstraAlgorithm(List<Edge>[] adjacencyList, int sourceNode)
    {
        var priorityQueue = new PriorityQueue<int, long>();

        // Distance array: stores shortest distance from source
        var distances = new long[adjacencyList.Length];
        var nextHops = new int?[adjacencyList.Length];
        for (var i = 0; i < adjacencyList.Length; i++)
        {
            distances[i] = int.MaxValue;
        }

        // Distance from source to itself is 0
        distances[sourceNode] = 0;
        priorityQueue.Enqueue(sourceNode, 0);

        // Process the queue until all reachable vertices are finalized
        while (priorityQueue.Count > 0)
        {
            _ = priorityQueue.TryDequeue(out int node, out long distance);

            // If this distance is not the latest shortest one, skip it
            if (distance > distances[node])
                continue;

            // Explore all adjacent vertices
            foreach (var p in adjacencyList[node])
            {
                var nextNode = p.EndNode;
                var weight = p.Weight;

                // If we found a shorter path to v through u, update it
                if (distances[node] + weight < distances[nextNode])
                {
                    distances[nextNode] = distances[node] + weight;
                    if (node == sourceNode)
                        nextHops[nextNode] = nextNode;
                    else
                        nextHops[nextNode] = nextHops[node];
                    priorityQueue.Enqueue(nextNode, distances[nextNode]);
                }
            }
        }

        return nextHops;
    }

    public Connection GetCurrentConnection() => _options.CurrentConnection;
    
    private record struct Edge(int EndNode, long Weight);
}
