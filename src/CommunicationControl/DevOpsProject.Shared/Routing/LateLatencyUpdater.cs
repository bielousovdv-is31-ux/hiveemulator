using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.Shared.Routing;

public sealed class LateLatencyUpdater(ILogger<LateLatencyUpdater> logger, IOptions<NetworkStatusPublisherOptions> options, IRouterService routerService, IOptions<RouterServiceOptions> routerServiceOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting LateLatencyUpdater");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(options.Value.Delay, stoppingToken);

                var currentTime = DateTimeOffset.UtcNow;
                routerService.UpdateLatencies(c =>
                {
                    var delta = currentTime - c.LastUpdatedAt;
                    if (delta > options.Value.Delay.Add(routerServiceOptions.Value.AdditionalLateDelay))
                    {
                        return currentTime - c.LastUpdatedAt;
                    }

                    return c.Latency;
                });
            }
            catch (OperationCanceledException operationCanceledException) when (
                operationCanceledException.CancellationToken == stoppingToken || stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error in LateLatencyUpdater");
            }
        }
        
        logger.LogInformation("Stopping LateLatencyUpdater");
    }
}
