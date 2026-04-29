using System.Diagnostics;

namespace Dotnetstore.MinimalApi.Api.WebApi.Filters;

internal sealed class LogPerformanceFilter(
    ILogger<LogPerformanceFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, 
        EndpointFilterDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation("Endpoint execution time: {ExecutionTime} ms", stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}