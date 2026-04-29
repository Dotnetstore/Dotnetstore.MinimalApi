using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Microsoft.Extensions.Options;

namespace Dotnetstore.MinimalApi.Api.WebApi.Filters;

internal sealed class ApiKeyFilter(
    IOptions<ApiKeySecurityOptions> options) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, 
        EndpointFilterDelegate next)
    {
        if (string.IsNullOrWhiteSpace(options.Value.HeaderName) || 
           string.IsNullOrWhiteSpace(options.Value.Value))
        {
            return ValueTask.FromResult<object?>(Results.Unauthorized());
        }
        
        if (!context.HttpContext.Request.Headers.TryGetValue(options.Value.HeaderName, out var providedApiKey))
        {
            return ValueTask.FromResult<object?>(Results.Unauthorized());
        }
        
        if(!string.Equals(providedApiKey, options.Value.Value, StringComparison.Ordinal))
        {
            return ValueTask.FromResult<object?>(Results.Unauthorized());
        }
        
        return next(context);
    }
}