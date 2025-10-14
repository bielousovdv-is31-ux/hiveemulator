using DevOpsProject.CommunicationControl.API.DI;
using DevOpsProject.CommunicationControl.API.Middleware;
using Microsoft.OpenApi.Models;
using Serilog;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, loggerConfig) =>
            loggerConfig.ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext());

        builder.Services.AddApiVersioningConfiguration();

        // TODO: consider this approach
        builder.Services.AddJsonControllerOptionsConfiguration();

        string basePath = builder.Configuration.GetValue<string>("BasePath") ?? string.Empty;
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "CommunicationControl - V1", Version = "v1.0" });
        });

        // TODO: LATER - ADD OpenTelemtry

        builder.Services.AddRedis(builder.Configuration);
        builder.Services.AddCommunicationControlLogic();

        builder.Services.AddOptionsConfiguration(builder.Configuration);

        builder.Services.AddHttpClientsConfiguration();

        var corsPolicyName = "AllowReactApp";
        builder.Services.AddCorsConfiguration(corsPolicyName);

        builder.Services.AddExceptionHandler<ExceptionHandlingMiddleware>();
        builder.Services.AddProblemDetails();

        var app = builder.Build();
        if (!string.IsNullOrEmpty(basePath))
        {
            var pathBase = new PathString(basePath);
            app.UsePathBase(pathBase);
            app.Use(async (context, next) =>
            {
                context.Request.PathBase = pathBase;
                await next();
            });
        }
        
        app.UseExceptionHandler();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"{basePath}/swagger/v1/swagger.json", "CommunicationControl - V1");
            c.RoutePrefix = $"swagger";
            
        });
        app.UseCors(corsPolicyName);

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}