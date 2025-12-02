using Common;
using DevOpsProject.Drone.API;
using DevOpsProject.Drone.API.Services;
using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Routing;
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
builder.Services.AddRouterService((opt, sp) =>
{
    opt.RouterUpdatedDelay = builder.Configuration.GetValue<TimeSpan>("RouterServiceOptions:RouterUpdatedDelay");
    opt.IsAliveCheckerDelay = builder.Configuration.GetValue<TimeSpan>("RouterServiceOptions:IsAliveCheckerDelay");
    opt.IsAliveCheckerMaxDifference = builder.Configuration.GetValue<TimeSpan>("RouterServiceOptions:IsAliveCheckerMaxDifference");
    opt.CurrentConnectionNameProvider = () =>
        Connection.GetName(sp.GetRequiredService<IDroneState>().DroneId, ConnectionType.Drone);
});
builder.Services.AddOptions<DroneInitialStateOptions>()
    .BindConfiguration("DroneInitialState")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IDroneState, DroneState>();
builder.Services.AddUdpService(builder.Configuration);
builder.Services.AddUdpListener();
builder.Services.AddUdpMessageHandler<NetworkStatus, NetworkStatusHandler>();
builder.Services.AddOptions<NetworkStatusPublisherOptions>()
    .BindConfiguration("NetworkStatusPublisherOptions")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<NetworkStatusPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<DroneGrpcService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
