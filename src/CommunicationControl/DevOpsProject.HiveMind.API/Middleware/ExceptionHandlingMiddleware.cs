using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;
using DevOpsProject.HiveMind.Logic.Exceptions;

namespace DevOpsProject.HiveMind.API.Middleware
{
    public class ExceptionHandlingMiddleware : IExceptionHandler
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var baseException = exception.GetBaseException();
            var (statusCode, message, details) = baseException switch
            {
                DroneRequestFailedException => (StatusCodes.Status400BadRequest, baseException.Message, null),
                _ => (StatusCodes.Status500InternalServerError, "Unexpected error occured",
                    _hostEnvironment.IsDevelopment() ? baseException.ToString() : null)
            };
            
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            var errorResponse = new
            {
                Message = message,
                Detail = details
            };

            if (httpContext.Response.StatusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception occured: {Message}", exception.Message);
            }
            
            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
            await httpContext.Response.WriteAsync(jsonResponse, cancellationToken);
            return true;
        }
    }
}
