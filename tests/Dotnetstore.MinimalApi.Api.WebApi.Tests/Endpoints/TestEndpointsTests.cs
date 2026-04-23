using System.Net;
using Asp.Versioning;
using Dotnetstore.MinimalApi.Api.WebApi.Endpoints;
using Dotnetstore.MinimalApi.Api.WebApi.Handlers;
using Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Endpoints;

/// <summary>
/// Covers <c>TestEndpoints</c> and verifies mapped route metadata and the endpoint response contract.
/// </summary>
public sealed class TestEndpointsTests
{
    private const string TestPath = "/test";

    [Fact]
    public async Task MapEndpoints_ShouldAddVersionedTestRoute_WhenCalled()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = TestApplication.CreateVersionedApp();
        ITestEndpoints sut = new TestEndpoints(new WebApplicationHandlers());
        var expectedApiVersion = new ApiVersion(1, 0);

        // Act
        sut.MapEndpoints(app);
        await app.StartAsync(cancellationToken);

        var endpoint = app.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .SingleOrDefault(candidate => candidate.RoutePattern.RawText == TestPath);

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

    [Fact]
    public async Task MapEndpoints_ShouldReturnHelloWorld_WhenCalledWithV1Request()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = TestApplication.CreateVersionedApp();
        ITestEndpoints sut = new TestEndpoints(new WebApplicationHandlers());
        sut.MapEndpoints(app);
        await app.StartAsync(cancellationToken);

        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateVersionedRequest(HttpMethod.Get, TestPath, "1.0");

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe("Hello World!");
    }
}

