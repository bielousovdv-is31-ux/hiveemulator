using DevOpsProject.HiveMind.Logic.Grpc;
using DevOpsProject.Shared.Grpc;
using Grpc.Core;
using Polly;
using Polly.Retry;

namespace DevOpsProject.HiveMind.API.DI;

public static class GrpcConfiguration
{
    public static IServiceCollection AddGrpcServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        services.AddGrpcClientFactory();
        services.AddResiliencePipeline("grpc-retry", (pipelineBuilder, context) =>
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
        services.AddSingleton<ResilienceInterceptor>();
        services.AddSingleton<LogHandleExceptionInterceptor>();
        
        return services;
    }
}