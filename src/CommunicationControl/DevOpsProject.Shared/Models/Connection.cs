using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models;

public sealed class Connection
{
    public string Name => GetName(DeviceId, Type);
    public string DeviceId { get; }
    public ConnectionType Type { get; }
    public string IpAddress { get; set; }
    public int Http1Port { get; set; }
    public int GrpcPort { get; set; }
    public int UdpPort { get; set; }
    public ConnectionState State { get; set; } = ConnectionState.Alive;
    public DateTimeOffset LastUpdatedAt { get; set; }
    public DateTimeOffset PreviousLastUpdatedAt { get; set; }

    public Uri Http1Uri
    {
        get
        {
            var builder = new UriBuilder(IpAddress)
            {
                Port = Http1Port
            };
            return builder.Uri;
        }
    }
    public Uri GrpcUri
    {
        get
        {
            var builder = new UriBuilder(IpAddress)
            {
                Port = GrpcPort
            };
            return builder.Uri;
        }
    }

    public Connection(string deviceId, ConnectionType type, string ipAddress, int http1Port, int grpcPort, int udpPort, DateTimeOffset lastUpdatedAt) 
    {
        DeviceId = deviceId;
        Type = type;
        IpAddress = ipAddress;
        Http1Port =  http1Port;
        GrpcPort = grpcPort;
        UdpPort = udpPort;
        LastUpdatedAt = lastUpdatedAt;
    }
    
    public static string GetName(string deviceId, ConnectionType type)
        => $"{type.ToString().ToLower()}:{deviceId}";
}
