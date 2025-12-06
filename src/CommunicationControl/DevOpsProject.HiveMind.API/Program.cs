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

builder.Services.AddGrpcServices();
builder.Services.AddMeshNetworkConfiguration(builder.Configuration);

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
