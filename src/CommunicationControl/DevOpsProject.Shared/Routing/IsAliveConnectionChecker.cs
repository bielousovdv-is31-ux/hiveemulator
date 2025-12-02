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
                
                routerService.WithReadLockedForEach(connection =>
                {
                    if (connection.State == ConnectionState.DeadNonRecoverable)
                    {
                        return;
                    }

                    var difference = connection.LastUpdatedAt - connection.PreviousLastUpdatedAt;
                    if (difference > maxDifference)
                    {
                        connection.State = ConnectionState.Dead;
                    }
                    else
                    {
                        connection.State = ConnectionState.Alive;
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occured while executing IsAliveConnectionChecker");
            }
        }
        
        logger.LogInformation("IsAliveConnectionChecker stopped");
    }
}
