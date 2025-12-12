using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;

namespace DevOpsProject.HiveMind.Logic.Grpc;

public sealed class DeadlineInterceptor(IOptions<DeadlineInterceptorOptions> deadlineInterceptorOptions) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            WithDeadline(context.Options));

        return continuation(request, newContext);
    }

    private CallOptions WithDeadline(CallOptions options)
    {
        return options.Deadline != null 
            ? options 
            : options.WithDeadline(DateTime.UtcNow.Add(deadlineInterceptorOptions.Value.Deadline));
    }
}
