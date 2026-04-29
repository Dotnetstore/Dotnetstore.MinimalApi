using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Dotnetstore.MinimalApi.Api.WebApi.OpenApi;

internal sealed class TestDocumentTransformer(
    IOptions<ApiKeySecurityOptions> options) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document, 
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["ApiKey"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = options.Value.HeaderName,
                In = ParameterLocation.Header
            }
        };

        foreach (var pathsValue in document.Paths.Values)
        {
            if (pathsValue.Operations is null) continue;

            foreach (var pathsOperation in pathsValue.Operations)
            {
                pathsOperation.Value.Security ??= [];
                
                pathsOperation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("ApiKey", document)] = []
                });
            }
        }
        
        return Task.CompletedTask;
    }
}