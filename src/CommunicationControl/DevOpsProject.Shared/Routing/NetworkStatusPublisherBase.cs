using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.Shared.Routing;

public abstract class NetworkStatusPublisherBase(ILogger<NetworkStatusPublisherBase> logger, IOptions<NetworkStatusPublisherOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting NetworkStatusPublisher");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(options.Value.Delay, stoppingToken);

                await PublishStatusAsync();
            }
            catch (OperationCanceledException operationCanceledException) when (
                operationCanceledException.CancellationToken == stoppingToken || stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error in NetworkStatusPublisher");
            }
        }
        
        logger.LogInformation("Stopping NetworkStatusPublisher");
    }

    protected abstract Task PublishStatusAsync();
}
