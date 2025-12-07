using DevOpsProject.HiveMind.Logic.Grpc;
using DevOpsProject.Shared.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace DevOpsProject.HiveMind.API.DI;

public static class GrpcConfiguration
{
    public static IServiceCollection AddGrpcServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        services.AddOptions<GrpcResilienceOptions>()
            .BindConfiguration("GrpcResilienceOptions")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddGrpcClientFactory();
        services.AddResiliencePipeline("grpc-retry", (pipelineBuilder, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<GrpcResilienceOptions>>().Value;
            pipelineBuilder.AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<RpcException>(ex =>
                        ex.StatusCode == StatusCode.Unavailable ||
                        ex.StatusCode == StatusCode.Aborted ||
                        ex.StatusCode == StatusCode.ResourceExhausted),
        
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = options.InitialDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            });
        });
        services.AddSingleton<LogHandleExceptionInterceptor>();
        
        return services;
    }
}