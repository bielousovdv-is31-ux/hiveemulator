using System.Collections.Concurrent;
using System.Reflection;
using DevOpsProject.Drone.Logic.State;
using DevOpsProject.Shared.Grpc;
using DevOpsProject.Shared.Routing;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using Polly;

namespace DevOpsProject.Drone.API;

public sealed class ReroutingGrpcInterceptor(IGrpcChannelFactory grpcChannelFactory, IRouterService routerService, IDroneState droneState, ILogger<ReroutingGrpcInterceptor> logger, IOptions<GrpcResilienceOptions> options) : Interceptor
{
    private static readonly ConcurrentDictionary<string, object> MethodCache = new();

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var destination = context.RequestHeaders.FirstOrDefault(h => h.Key == RoutingConstants.DestinationHeaderName)?.Value;
        if (string.IsNullOrEmpty(destination) || destination.Equals(droneState.Name))
        {
            return await continuation(request, context);
        }
        
        var headers = context.RequestHeaders;
        var previousHopHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == RoutingConstants.PreviousHopHeaderName);
        if (previousHopHeader != null)
        {
            headers.Remove(previousHopHeader);
            headers.Add(RoutingConstants.PreviousHopHeaderName, routerService.GetCurrentConnection().Name);
        }

        var methodDefinition = (Method<TRequest, TResponse>) MethodCache.GetOrAdd(
            context.Method, 
            CreateMethodDefinition<TRequest, TResponse>);
        
        var callOptions = new CallOptions(headers, context.Deadline, context.CancellationToken);
        
        var retryPolicy = Policy
            .Handle<RpcException>(ex =>
                ex.StatusCode == StatusCode.Unavailable ||
                ex.StatusCode == StatusCode.ResourceExhausted ||
                ex.StatusCode == StatusCode.Aborted)
            .WaitAndRetryAsync(options.Value.MaxRetryAttempts, i => options.Value.InitialDelay * Math.Pow(2, i));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var destinationConnection = routerService.GetNextHop(destination);
            if (destinationConnection == null)
            {
                throw new InvalidOperationException($"Destination {destination} is not reachable from this drone.");
            }
            logger.LogInformation("Redirecting gRPC message {MessageType} to {Destination}", request.GetType(), destinationConnection.Name);
        
            var channel = grpcChannelFactory.Create(destinationConnection.GrpcUri);
            var invoker = channel.CreateCallInvoker();
            
            using var call = invoker.AsyncUnaryCall(
                methodDefinition,
                null, 
                callOptions, 
                request);
            return await call.ResponseAsync;
        });
    }

    private static Method<TRequest, TResponse> CreateMethodDefinition<TRequest, TResponse>(string fullMethodName)
    {
        var parts = fullMethodName.Split('/'); 

        var serviceName = parts[1];
        var methodName = parts[2];
        
        var requestParser = GetParser<TRequest>();
        var responseParser = GetParser<TResponse>();
        
        var methodDefinition = new Method<TRequest, TResponse>(
            MethodType.Unary,
            serviceName,
            methodName,
            Marshallers.Create(
                (arg) => ((IMessage)arg!).ToByteArray(),
                (bytes) => (TRequest)requestParser.ParseFrom(bytes)
            ),
            Marshallers.Create(
                (arg) => ((IMessage)arg!).ToByteArray(),
                (bytes) => (TResponse)responseParser.ParseFrom(bytes)
            )
        );
        
        return methodDefinition;
    }
    
    private static MessageParser GetParser<T>()
    {
        var property = typeof(T).GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
        
        if (property == null)
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} is not a standard Protobuf message (missing static Parser).");
        }

        return (MessageParser)property.GetValue(null)!;
    }
}
