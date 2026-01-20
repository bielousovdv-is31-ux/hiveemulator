using System.Net;
using DevOpsProject.HiveMind.API.DronesTelemetryLogging;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using Listener;
using Microsoft.Extensions.Options;

namespace DevOpsProject.HiveMind.API.DI;

public static class MeshNetworkConfiguration
{
    public static IServiceCollection AddMeshNetworkConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        
        services.AddRouterService(configuration, (opt, sp) =>
        {
            opt.RouterUpdaterDelay = configuration.GetValue<TimeSpan>("RouterServiceOptions:RouterUpdaterDelay");
            opt.IsAliveCheckerDelay = configuration.GetValue<TimeSpan>("RouterServiceOptions:IsAliveCheckerDelay");
            opt.IsAliveCheckerMaxDifference = configuration.GetValue<TimeSpan>("RouterServiceOptions:IsAliveCheckerMaxDifference");
    
            var currentUri = new Uri((configuration["urls"]
                                      ?? configuration["ASPNETCORE_URLS"]!).Split(';', StringSplitOptions.RemoveEmptyEntries)[0]);
            var httpGrpcPort = currentUri.Port;
            var udpPort = ushort.Parse(Environment.GetEnvironmentVariable("UDP_PORT")!);
            var ipAddress = Environment.GetEnvironmentVariable("IP_ADDRESS");
            if (string.IsNullOrEmpty(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
            {
                throw new InvalidOperationException("Provide a valid IP_ADDRESS");
            }
            opt.CurrentConnection = new Connection(
                sp.GetRequiredService<IOptions<HiveCommunicationConfig>>().Value.HiveID,
                Shared.Enums.ConnectionType.Hive,
                ipAddress,
                httpGrpcPort,
                httpGrpcPort,
                udpPort,
                DateTimeOffset.UtcNow);
        });

        services.AddNetworkStatusPublisher<NetworkStatusPublisher>();

        services.AddUdpMessageHandler<DroneTelemetry, DroneTelemetryHandler>();
        services.AddHostedService<DronesTelemetryLogger>();
        services.AddOptions<DronesTelemetryLoggerOptions>()
            .BindConfiguration("DronesTelemetryLoggerOptions")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        return services;
    }
}
