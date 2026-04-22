using Asp.Versioning.Builder;

namespace Dotnetstore.MinimalApi.Api.WebApi.Handlers;

internal interface IWebApplicationHandlers
{
    ApiVersionSet GetApiVersionSet(WebApplication app);
}