using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Dotnetstore.MinimalApi.Api.WebApi.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.OpenApi;

public sealed class TestDocumentTransformerTests
{
    private const string ApiKeySchemeName = "ApiKey";
    private const string HeaderName = "X-TEST-API-KEY";

    [Fact]
    public async Task TransformAsync_ShouldCreateComponentsAndConfigureApiKeySecurityScheme_WhenComponentsAreMissing()
    {
        // Arrange
        var sut = CreateSut(HeaderName);
        var document = new OpenApiDocument
        {
            Components = null
        };
        document.Paths.Add("/ping", new OpenApiPathItem());

        // Act
        await sut.TransformAsync(document, null!, TestContext.Current.CancellationToken);

        // Assert
        var components = document.Components.ShouldNotBeNull();
        var securitySchemes = components.SecuritySchemes.ShouldNotBeNull();
        securitySchemes.ShouldContainKey(ApiKeySchemeName);

        var apiKeyScheme = securitySchemes[ApiKeySchemeName].ShouldBeOfType<OpenApiSecurityScheme>();
        apiKeyScheme.Type.ShouldBe(SecuritySchemeType.ApiKey);
        apiKeyScheme.Name.ShouldBe(HeaderName);
        apiKeyScheme.In.ShouldBe(ParameterLocation.Header);
    }

    [Fact]
    public async Task TransformAsync_ShouldAddApiKeyRequirement_ToEachOperationWithNullSecurity()
    {
        // Arrange
        var sut = CreateSut(HeaderName);
        var document = new OpenApiDocument();
        var pathItem = new OpenApiPathItem();
        pathItem.AddOperation(HttpMethod.Get, new OpenApiOperation());
        pathItem.AddOperation(HttpMethod.Post, new OpenApiOperation());
        document.Paths.Add("/ping", pathItem);

        // Act
        await sut.TransformAsync(document, null!, TestContext.Current.CancellationToken);

        // Assert
        var operations = document.Paths["/ping"].Operations.ShouldNotBeNull();
        operations.Count.ShouldBe(2);

        foreach (var operation in operations.Values)
        {
            AssertApiKeyRequirement(operation);
        }
    }

    [Fact]
    public async Task TransformAsync_ShouldPreserveExistingSecurityAndSkipPathsWithoutOperations()
    {
        // Arrange
        var sut = CreateSut(HeaderName);
        var document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["ExistingScheme"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        Name = "Authorization",
                        In = ParameterLocation.Header
                    }
                }
            }
        };
        var existingRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("ExistingScheme", document)] = []
        };
        var securedPathItem = new OpenApiPathItem();
        securedPathItem.AddOperation(HttpMethod.Get, new OpenApiOperation
        {
            Security = [existingRequirement]
        });
        document.Paths.Add("/secured", securedPathItem);
        document.Paths.Add("/empty", new OpenApiPathItem());

        // Act
        var exception = await Record.ExceptionAsync(() => sut.TransformAsync(document, null!, TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeNull();

        var securedOperation = document.Paths["/secured"].Operations.ShouldNotBeNull().Values.Single();
        var security = securedOperation.Security.ShouldNotBeNull();
        security.Count.ShouldBe(2);
        security[0].ShouldBe(existingRequirement);
        AssertApiKeyRequirement(security[1]);
        document.Paths["/empty"].Operations.ShouldBeNull();
    }

    private static TestDocumentTransformer CreateSut(string headerName) =>
        new(Options.Create(new ApiKeySecurityOptions
        {
            HeaderName = headerName,
            Value = "unused-for-transformer-tests"
        }));

    private static void AssertApiKeyRequirement(OpenApiOperation operation)
    {
        var security = operation.Security.ShouldNotBeNull();
        security.Count.ShouldBeGreaterThan(0);

        AssertApiKeyRequirement(security.Single());
    }

    private static void AssertApiKeyRequirement(OpenApiSecurityRequirement requirement)
    {
        requirement.Count.ShouldBe(1);

        requirement.Keys.Single().ShouldBeOfType<OpenApiSecuritySchemeReference>();

        var scopes = requirement.Values.Single();
        scopes.ShouldBeEmpty();
    }
}

