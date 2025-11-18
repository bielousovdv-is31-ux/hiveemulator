using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models;

public sealed class Connection
{
    public string Name { get; }
    public ConnectionType Type { get; }
    public Uri Http1Uri { get; }
    public Uri GrpcUri { get; }

    public Connection(string name, ConnectionType type, Uri http1Uri, Uri grpcUri)
    {
        Name = name;
        Type = type;
        Http1Uri = http1Uri;
        GrpcUri = grpcUri;
    }
}
