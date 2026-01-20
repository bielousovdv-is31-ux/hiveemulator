using System.Collections.Concurrent;
using Grpc.Net.Client;

namespace DevOpsProject.Shared.Grpc;

public sealed class GrpcChannelFactory : IGrpcChannelFactory, IDisposable
{
    private ConcurrentDictionary<string, GrpcChannel> _channels = new();
    
    public GrpcChannel Create(string url)
    {
        var uri = new Uri(url);

        return Create(uri);
    }

    public GrpcChannel Create(Uri uri)
    {
        return _channels.GetOrAdd(uri.AbsoluteUri, GrpcChannel.ForAddress);
    }

    public void Dispose()
    {
        foreach (var channel in _channels.Values)
        {
            channel.Dispose();
        }
        
        _channels = null!;
    }
}
