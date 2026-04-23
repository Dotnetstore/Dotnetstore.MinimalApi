using Asp.Versioning;
using Dotnetstore.MinimalApi.Api.WebApi.Handlers;
using Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Handlers;

/// <summary>
/// Covers <c>WebApplicationHandlers</c> and verifies API version set construction and handler-level version metadata behavior.
/// </summary>
public sealed class WebApplicationHandlersTests
{
    [Fact]
    public void GetApiVersionSet_ShouldReturnApiVersionSet_WhenCalled()
    {
        // Arrange
        using var app = TestApplication.CreateVersionedApp();
        IWebApplicationHandlers sut = new WebApplicationHandlers();

        // Act
        var versionSet = sut.GetApiVersionSet(app);

        // Assert
        versionSet.ShouldNotBeNull();
    }

    [Fact]
    public void GetApiVersionSet_ShouldBuildVersionModel_WhenVersionOneZeroIsConfigured()
    {
        // Arrange
        using var app = TestApplication.CreateVersionedApp();
        IWebApplicationHandlers sut = new WebApplicationHandlers();
        var expectedApiVersion = new ApiVersion(1, 0);

        // Act
        var versionSet = sut.GetApiVersionSet(app);
        var versionModel = versionSet.Build(new ApiVersioningOptions());

        // Assert
        versionModel.IsApiVersionNeutral.ShouldBeFalse();
        versionModel.DeclaredApiVersions.ShouldHaveSingleItem().ShouldBe(expectedApiVersion);
        versionModel.ImplementedApiVersions.ShouldHaveSingleItem().ShouldBe(expectedApiVersion);
        versionModel.SupportedApiVersions.ShouldHaveSingleItem().ShouldBe(expectedApiVersion);
        versionModel.DeprecatedApiVersions.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetApiVersionSet_ShouldApplyVersionMetadata_WhenUsedByEndpoint()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = TestApplication.CreateVersionedApp();
        IWebApplicationHandlers sut = new WebApplicationHandlers();
        var expectedApiVersion = new ApiVersion(1, 0);

        // Act
        app.MapGet("/versioned", () => "ok")
           .WithApiVersionSet(sut.GetApiVersionSet(app))
           .MapToApiVersion(expectedApiVersion);

        await app.StartAsync(cancellationToken);

        var endpoint = app.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .SingleOrDefault(candidate => candidate.RoutePattern.RawText == "/versioned");

        var metadata = endpoint?
            .Metadata
            .OfType<ApiVersionMetadata>()
            .SingleOrDefault();

        // Assert
        endpoint.ShouldNotBeNull();
        metadata.ShouldNotBeNull();
        metadata.Map(ApiVersionMapping.Explicit).IsApiVersionNeutral.ShouldBeFalse();
        metadata.Map(ApiVersionMapping.Explicit).DeclaredApiVersions.ShouldHaveSingleItem().ShouldBe(expectedApiVersion);
        metadata.Map(ApiVersionMapping.Explicit).ImplementedApiVersions.ShouldHaveSingleItem().ShouldBe(expectedApiVersion);
        metadata.Map(ApiVersionMapping.Explicit).SupportedApiVersions.ShouldHaveSingleItem().ShouldBe(expectedApiVersion);
        metadata.Map(ApiVersionMapping.Explicit).DeprecatedApiVersions.ShouldBeEmpty();
    }
}

