using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.Shared.Routing;

public sealed class RouterUpdaterBackgroundService(ILogger<RouterUpdaterBackgroundService> logger, IRouterService routerService, IOptions<RouterServiceOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RouterUpdaterBackgroundService started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(options.Value.RouterUpdaterDelay, stoppingToken);

                routerService.RecalculateHops();
            }
            catch (OperationCanceledException operationCanceledException) when (
                operationCanceledException.CancellationToken == stoppingToken || stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occured while executing router updater");
            }
        }
        
        logger.LogInformation("RouterUpdaterBackgroundService stopped");
    }
}
