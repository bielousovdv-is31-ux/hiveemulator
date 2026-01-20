using Grpc.Net.Client;

namespace DevOpsProject.Shared.Grpc;

public interface IGrpcChannelFactory
{
    GrpcChannel Create(string url);
    GrpcChannel Create(Uri url);
}
