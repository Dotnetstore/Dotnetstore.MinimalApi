using System.Net;
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
    public async Task Program_ShouldStartSuccessfully_WhenSingletonTestEndpointIsRegistered()
    {
        // Arrange
        await using var factory = CreateFactory(Environments.Production);
        using var client = TestHttp.CreateClient(factory, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateVersionedRequest(HttpMethod.Get, TestPath, "1.0");

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

