using Asp.Versioning.Builder;
using Asp.Versioning.Conventions;

namespace Dotnetstore.MinimalApi.Api.WebApi.Handlers;

internal sealed class WebApplicationHandlers : IWebApplicationHandlers
{
    ApiVersionSet IWebApplicationHandlers.GetApiVersionSet(WebApplication app)
    {
        return app
            .NewApiVersionSet()
            .HasApiVersion(1, 0)
            .Build();
    }
}