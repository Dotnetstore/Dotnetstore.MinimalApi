using Dotnetstore.MinimalApi.Api.WebApi.Filters;
using Dotnetstore.MinimalApi.Api.WebApi.Handlers;

namespace Dotnetstore.MinimalApi.Api.WebApi.Endpoints;

internal sealed class TestEndpoints(
    IWebApplicationHandlers webApplicationHandlers) : ITestEndpoints
{
    void ITestEndpoints.MapEndpoints(WebApplication app)
    {
        app.MapGet("/test", () => "Hello World!")
            .WithApiVersionSet(webApplicationHandlers.GetApiVersionSet(app))
            .AddEndpointFilter<LogPerformanceFilter>()
            .AddEndpointFilter<ApiKeyFilter>()
            .MapToApiVersion(1.0);
    }
}