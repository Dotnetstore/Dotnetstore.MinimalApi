using System.Diagnostics;

namespace Dotnetstore.MinimalApi.Api.WebApi.Filters;

internal sealed class LogPerformanceFilter(
    ILogger<LogPerformanceFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, 
        EndpointFilterDelegate next)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var result = await next(context);

        stopwatch.Stop();
        logger.LogInformation("Endpoint execution time: {ExecutionTime} ms", stopwatch.ElapsedMilliseconds);

        return result;
    }
}