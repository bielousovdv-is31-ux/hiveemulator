using Common;
using DevOpsProject.Shared.Grpc;
using Listener;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsProject.Shared.Routing;

public static class RouterServiceExtensions
{
    public static IServiceCollection AddRouterService(this IServiceCollection services, IConfiguration configuration, Action<RouterServiceOptions, IServiceProvider> configure)
    {
        services.AddOptions<RouterServiceOptions>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IRouterService, RouterService>();
        services.AddHostedService<RouterUpdaterBackgroundService>();
        services.AddHostedService<IsAliveConnectionChecker>();

        services.AddUdpService(configuration);
        services.AddUdpListener(opt => opt.IgnoreConnectionResetErrors = true);

        services.AddUdpMessageHandler<NetworkStatus, NetworkStatusHandler>();
        
        return services;
    }
}
