using Microsoft.Extensions.DependencyInjection;

namespace DevOpsProject.Shared.Grpc;

public static class GrpcChannelFactoryExtensions
{
    public static IServiceCollection AddGrpcClientFactory(this IServiceCollection services)
    {
        return services.AddSingleton<IGrpcChannelFactory, GrpcChannelFactory>();
    }
}
