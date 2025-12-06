using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models;

public sealed record Connection(string DeviceId, ConnectionType Type, string IpAddress, int Http1Port, int GrpcPort, int UdpPort, DateTimeOffset LastUpdatedAt) 
{
    public string Name => GetName(DeviceId, Type);
    public ConnectionState State { get; init; } = ConnectionState.Alive;

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
    
    public static string GetName(string deviceId, ConnectionType type)
        => $"{type.ToString().ToLower()}:{deviceId}";
}
