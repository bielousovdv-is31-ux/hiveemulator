using DevOpsProject.Shared.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.Shared.Routing;

public class IsAliveConnectionChecker(ILogger<IsAliveConnectionChecker> logger, IOptions<RouterServiceOptions> options, IRouterService routerService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IsAliveConnectionChecker started");
        var maxDifference = options.Value.IsAliveCheckerMaxDifference;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(options.Value.IsAliveCheckerDelay, stoppingToken);
                
                routerService.UpdateConnectionForEach(connection =>
                {
                    if (connection.State == ConnectionState.DeadNonRecoverable
                        || connection == options.Value.CurrentConnection)
                    {
                        return;
                    }

                    var difference = connection.LastUpdatedAt - connection.PreviousLastUpdatedAt;
                    connection.State = difference > maxDifference ? ConnectionState.Dead : ConnectionState.Alive;
                });
            }
            catch (OperationCanceledException operationCanceledException) when (
                operationCanceledException.CancellationToken == stoppingToken || stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occured while executing IsAliveConnectionChecker");
            }
        }
        
        logger.LogInformation("IsAliveConnectionChecker stopped");
    }
}
