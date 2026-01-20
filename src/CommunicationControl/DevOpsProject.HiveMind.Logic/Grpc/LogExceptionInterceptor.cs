using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Grpc;

public sealed class LogExceptionInterceptor(ILogger<LogExceptionInterceptor> logger) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var call = continuation(request, context);

        async Task<TResponse> LogResponseAsync()
        {
            try
            {
                var response = await call.ResponseAsync.ConfigureAwait(false);

                return response;
            }
            catch (RpcException ex)
            {
                logger.LogError(
                    ex,
                    "gRPC call failed. Method: {Method}, StatusCode: {StatusCode}, Detail: {Detail}",
                    context.Method.FullName,
                    ex.StatusCode,
                    ex.Status.Detail);

                throw;
            }
        }

        return new AsyncUnaryCall<TResponse>(
            LogResponseAsync(),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }
}
