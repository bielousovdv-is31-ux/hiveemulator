using DevOpsProject.Shared.Routing;
using DevOpsProject.Shared.Simulation;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace DevOpsProject.Drone.API;

public sealed class SimulationGrpcInterceptor(ISimulationUtility simulationUtility, ILogger<SimulationGrpcInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var previousHopHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == RoutingConstants.PreviousHopHeaderName);
        if (previousHopHeader != null)
        {
            var simulationLatency = simulationUtility.BadDeviceLatency;
            if (simulationLatency.HasValue)
            {
                await Task.Delay(simulationLatency.Value);
            }
            
            var connectionSimulationLatency = simulationUtility.GetBadConnectionLatency(previousHopHeader.Value);
            if (connectionSimulationLatency.HasValue)
            {
                await Task.Delay(connectionSimulationLatency.Value);
            }
            
            logger.LogWarning("Simulation - failing the connection.");
            throw new RpcException(new Status(StatusCode.Unavailable, "The service is currently unavailable. Please try again later."));
        }
        
        return await continuation(request, context);
    }
}
