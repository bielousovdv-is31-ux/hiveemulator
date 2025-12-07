using System.Net;
using Common;
using DevOpsProject.Drone.API;
using DevOpsProject.Drone.API.Services;
using DevOpsProject.Drone.Logic.Services.Interfaces;
using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
using DevOpsProject.Shared.Simulation;
using Listener;
using ConnectionType = DevOpsProject.Shared.Enums.ConnectionType;
using DroneState = DevOpsProject.Drone.Logic.State.DroneState;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc(opt =>
{
    opt.Interceptors.Add<ReroutingGrpcInterceptor>();
});
builder.Services.AddGrpcClientFactory();

builder.Services.AddOptions<DroneInitialStateOptions>()
    .BindConfiguration("DroneInitialState")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IDroneState, DroneState>();
builder.Services.AddSingleton<IDroneService, DevOpsProject.Drone.Logic.Services.DroneService>();

builder.Services.AddSimulationUtility();

builder.Services.AddRouterService(builder.Configuration, (opt, sp) =>
{
    opt.RouterUpdaterDelay = builder.Configuration.GetValue<TimeSpan>("RouterServiceOptions:RouterUpdaterDelay");
    opt.IsAliveCheckerDelay = builder.Configuration.GetValue<TimeSpan>("RouterServiceOptions:IsAliveCheckerDelay");
    opt.IsAliveCheckerMaxDifference = builder.Configuration.GetValue<TimeSpan>("RouterServiceOptions:IsAliveCheckerMaxDifference");
    
    var currentUri = new Uri((builder.Configuration["urls"]
                           ?? builder.Configuration["ASPNETCORE_URLS"]!).Split(';', StringSplitOptions.RemoveEmptyEntries)[0]);
    var httpGrpcPort = currentUri.Port;
    var udpPort = ushort.Parse(Environment.GetEnvironmentVariable("UDP_PORT")!);
    var ipAddress = Environment.GetEnvironmentVariable("IP_ADDRESS");
    if (string.IsNullOrEmpty(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
    {
        throw new InvalidOperationException("Provide a valid IP_ADDRESS");
    }
    opt.CurrentConnection = new Connection(
        sp.GetRequiredService<IDroneState>().DroneId,
        ConnectionType.Drone,
        ipAddress,
        httpGrpcPort,
        httpGrpcPort,
        udpPort,
        DateTimeOffset.UtcNow);
});

builder.Services.AddUdpService(builder.Configuration);

builder.Services.AddOptions<NetworkStatusPublisherOptions>()
    .BindConfiguration("NetworkStatusPublisherOptions")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<NetworkStatusPublisher>();

builder.Services.AddOptions<DroneTelemetryPublisherOptions>()
    .BindConfiguration("DroneTelemetryPublisherOptions")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<DroneTelemetryPublisher>();

builder.Services.AddUdpMessageHandler<DroneTelemetry, DroneTelemetryReroutingHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<DroneGrpcService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
