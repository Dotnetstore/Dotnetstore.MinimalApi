using System.Net;
using System.Text.Json;
using Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests;

/// <summary>
/// Covers the real <c>Program</c> startup path and verifies top-level application composition.
/// </summary>
public sealed class ProgramTests
{
    private const string HealthPath = "/health";
    private const string TestPath = "/test";

    [Fact]
    public async Task Program_ShouldReturnUnauthorized_WhenTestEndpointIsCalledWithoutApiKey()
    {
        // Arrange
        await using var factory = CreateFactory(Environments.Production);
        using var client = TestHttp.CreateClient(factory, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateVersionedRequest(HttpMethod.Get, TestPath, "1.0");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Program_ShouldStartSuccessfully_WhenSingletonTestEndpointIsRegisteredAndApiKeyIsValid()
    {
        // Arrange
        await using var factory = CreateFactory(Environments.Production);
        using var client = TestHttp.CreateClient(factory, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateAuthorizedVersionedRequest(HttpMethod.Get, TestPath, "1.0");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("Hello World!");
    }

    [Fact]
    public async Task Program_ShouldExposeHealthEndpoint_WhenRunningInDevelopmentWithAspireServiceDefaults()
    {
        // Arrange
        await using var factory = CreateFactory(Environments.Development);
        using var client = TestHttp.CreateClient(factory, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync(HealthPath, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Program_ShouldExposeOpenApiDocument_WhenRunningInDevelopment()
    {
        // Arrange
        await using var factory = CreateFactory(Environments.Development);
        using var client = TestHttp.CreateClient(factory, TestHttp.HttpsLocalhost);

        // Act
        using var response = await client.GetAsync(TestHttp.OpenApiDocumentPath, TestContext.Current.CancellationToken);
        await using var contentStream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        document.RootElement.GetProperty("components")
            .GetProperty("securitySchemes")
            .TryGetProperty("ApiKey", out _)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Program_ShouldNotExposeHealthEndpoint_WhenRunningInProduction()
    {
        // Arrange
        await using var factory = CreateFactory(Environments.Production);
        using var client = TestHttp.CreateClient(factory, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync(HealthPath, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static ProgramWebApplicationFactory CreateFactory(
        string environment,
        Action<IServiceCollection>? configureServices = null) =>
        new(environment, configureServices);


    private sealed class ProgramWebApplicationFactory(
        string environment,
        Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureServices(services => configureServices?.Invoke(services));
        }
    }
}

