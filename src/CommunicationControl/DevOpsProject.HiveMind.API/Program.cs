using System.Net;
using Asp.Versioning;
using Asp.Versioning.Builder;
using DevOpsProject.HiveMind.API;
using DevOpsProject.HiveMind.API.DI;
using DevOpsProject.HiveMind.API.DronesTelemetryLogging;
using DevOpsProject.HiveMind.API.Middleware;
using DevOpsProject.HiveMind.Logic.Grpc;
using DevOpsProject.HiveMind.Logic.Patterns.Factory.Interfaces;
using DevOpsProject.HiveMind.Logic.Services;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using DevOpsProject.Shared.Models.HiveMindCommands;
using DevOpsProject.Shared.Routing;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Listener;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Retry;
using Serilog;
using ConnectionType = DevOpsProject.Shared.Enums.ConnectionType;
using RetryPolicy = Polly.Retry.RetryPolicy;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

builder.Services.AddApiVersioningConfiguration();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HiveMind - V1", Version = "v1.0" });
});

builder.Services.AddOptionsConfiguration(builder.Configuration);

//builder.Services.AddValidatorsConfiguration();
builder.Services.AddHiveMindLogic();

builder.Services.AddHttpClientsConfiguration();

string corsPolicyName = "HiveMindCorsPolicy";
builder.Services.AddCorsConfiguration(corsPolicyName);

builder.Services.AddExceptionHandler<ExceptionHandlingMiddleware>();
builder.Services.AddProblemDetails();

builder.Services.AddGrpcClientFactory();
builder.Services.AddResiliencePipeline("grpc-retry", (pipelineBuilder, context) =>
{
    pipelineBuilder.AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder()
            .Handle<RpcException>(ex =>
                ex.StatusCode == StatusCode.Unavailable ||
                ex.StatusCode == StatusCode.Aborted ||
                ex.StatusCode == StatusCode.ResourceExhausted),
        
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(200),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    });
});
builder.Services.AddSingleton<ResilienceInterceptor>();
builder.Services.AddSingleton<LogHandleExceptionInterceptor>();

builder.Services.AddRouterService((opt, sp) =>
{
    opt.RouterUpdaterDelay = builder.Configuration.GetValue<TimeSpan>("RouterServiceOptions:RouterUpdatedDelay");
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
        sp.GetRequiredService<IOptions<HiveCommunicationConfig>>().Value.HiveID,
        ConnectionType.Hive,
        ipAddress,
        httpGrpcPort,
        httpGrpcPort,
        udpPort,
        DateTimeOffset.UtcNow);
});

builder.Services.AddOptions<NetworkStatusPublisherOptions>()
    .BindConfiguration("NetworkStatusPublisherOptions")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<NetworkStatusPublisher>();

builder.Services.AddUdpMessageHandler<DroneTelemetry, DroneTelemetryHandler>();
builder.Services.AddSingleton<IDroneTelemetryService, DroneTelemetryService>();
builder.Services.AddHostedService<DronesTelemetryLogger>();
builder.Services.AddOptions<DronesTelemetryLoggerOptions>()
    .BindConfiguration("DronesTelemetryLoggerOptions")
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var hiveMindService = scope.ServiceProvider.GetRequiredService<IHiveMindService>();
        await hiveMindService.ConnectHive();
    }
    catch (Exception ex)
    {
        logger.LogError($"Error occured while connecting Hive to Communication Control. \nException text: {ex.Message}");
        Environment.Exit(1);
    }
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(corsPolicyName);

//app.UseHttpsRedirection();

app.UseAuthorization();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();
RouteGroupBuilder groupBuilder = app.MapGroup("api/v{apiVersion:apiVersion}").WithApiVersionSet(apiVersionSet);

groupBuilder.MapGet("ping", (IOptionsSnapshot<HiveCommunicationConfig> config) =>
{
    return Results.Ok(new
    {
        Timestamp = DateTime.Now,
        ID = config.Value.HiveID
    });
});

groupBuilder.MapPost("command", async (HiveMindCommand command, [FromServices]ICommandHandlerFactory factory) =>
{
    var handler = factory.GetHandler(command);
    await handler.HandleAsync(command);
    return Results.Ok();
});

app.Run();
