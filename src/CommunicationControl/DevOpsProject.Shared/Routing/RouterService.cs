using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Routing;

public sealed class RouterService : IRouterService
{
    private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
    
    private Dictionary<string, ISet<string>> _lastConnectedDevicesSnapshot = new();
    private Dictionary<string, ISet<string>> _connectedDevices = new();
    private readonly Dictionary<string, Connection> _connections = new();
    
    private Dictionary<string, Connection> _nextHops = new();

    public Connection GetNextHop(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        
        _rwLock.EnterReadLock();
        try
        {
            return _nextHops[name];
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }
    
    public Connection GetConnection(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        
        _rwLock.EnterReadLock();
        try
        {
            return _connections[name];
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
    
    public ISet<string> GetConnectedDevicesNames(string name)
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

    public IReadOnlyList<Connection> GetConnections()
    {
        _rwLock.EnterReadLock();
        try
        {
            return _connections.Values.ToList();
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }
    
    public void AddConnection(Connection connection, ISet<string> connectedDevicesNames)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(connectedDevicesNames);
        
        _rwLock.EnterWriteLock();
        try
        {
            _connections.Add(connection.Name, connection);
            _connectedDevices.Add(connection.Name, connectedDevicesNames);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
    
    public void UpdateConnection(Connection connection, ISet<string> connectedDevicesNames)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(connectedDevicesNames);
        
        _rwLock.EnterWriteLock();
        try
        {
            if (!_connections.ContainsKey(connection.Name))
            {
                throw new KeyNotFoundException($"No connection with name {connection.Name} found.");
            }
            
            _connections[connection.Name] = connection;
            _connectedDevices[connection.Name] = connectedDevicesNames;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
    
    public void RemoveConnection(Connection connection, ISet<string> connectedDevicesNames)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(connectedDevicesNames);
        
        _rwLock.EnterWriteLock();
        try
        {
            if (!_connections.Remove(connection.Name))
            {
                throw new KeyNotFoundException($"No connection with name {connection.Name} found.");
            }
            
            _ = _connectedDevices.Remove(connection.Name);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
    
    public bool IsRecalculationNeeded()
    {
        _rwLock.EnterReadLock();
        try
        {
            if (_lastConnectedDevicesSnapshot.Count != _connectedDevices.Count ||
                _lastConnectedDevicesSnapshot.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < _lastConnectedDevicesSnapshot.Count; i++)
            {
                if (!_lastConnectedDevicesSnapshot.ElementAt(i).Value.SetEquals(_connectedDevices.ElementAt(i).Value))
                {
                    return true;
                }
            }
            
            return false;
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }
    
    public void RecalculateHops(string currentConnectionName)
    {
        ArgumentNullException.ThrowIfNull(currentConnectionName);
        
        _rwLock.EnterWriteLock();

        try
        {
            _lastConnectedDevicesSnapshot = _connectedDevices;

            var hiveMind = _connections
                .Select(kvp => kvp.Value)
                .FirstOrDefault(c => c.Type == ConnectionType.Hive);
            var hiveMindIsConnected = hiveMind is not null;
            _nextHops = new();
            if (hiveMindIsConnected)
            {
                _nextHops[hiveMind.Name] = null;
            }
            
            var currentConnectionIsHiveMind = hiveMindIsConnected && hiveMind.Name.Equals(currentConnectionName);
            var connections = currentConnectionIsHiveMind
                ? _connections.Values.ToList()
                : _connections.Select(c => c.Value)
                    .Where(c => c != hiveMind)
                    .ToList();
            
            RecalculateHopsInternal(connections, currentConnectionName);

            if (hiveMindIsConnected && !currentConnectionIsHiveMind)
            {
                if (_connectedDevices[currentConnectionName].Contains(hiveMind.Name))
                {
                    _nextHops[hiveMind.Name] = hiveMind;
                }
                else
                {
                    foreach (var (connectionName, devices) in _connectedDevices)
                    {
                        if (devices.Contains(hiveMind.Name))
                        {
                            _nextHops[hiveMind.Name] = _connections[connectionName];
                            break;
                        }
                    }
                }
            }
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }
    
    private void RecalculateHopsInternal(IReadOnlyList<Connection> connections, string currentConnectionName)
    {
        var connectionIndexes = connections
            .Select((c, i) =>  new { c, i })
            .ToDictionary(c => c.c.Name, c => c.i);
        
        var adjacencyList = new List<Edge>[connections.Count];
        var currentConnectionIndex = connectionIndexes[currentConnectionName];
        for (var i = 0; i < connections.Count; i++)
        {
            var connection = connections[i];

            var adjacentNodes = _connectedDevices[connection.Name]
                .Where(c => connectionIndexes.ContainsKey(c))
                .Select(c => new Edge(connectionIndexes[c], 1))
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
    
    private static int?[] GetNextHopsByDijkstraAlgorithm(List<Edge>[] adjacencyList, int sourceNode)
    {
        var priorityQueue = new PriorityQueue<int, int>();

        // Distance array: stores shortest distance from source
        var distances = new int[adjacencyList.Length];
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
            _ = priorityQueue.TryDequeue(out int node, out int distance);

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
    
    private record struct Edge(int EndNode, int Weight);
}
