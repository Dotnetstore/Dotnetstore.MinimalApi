using System.Net;
using Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests;

public sealed class ProgramTests
{
    private const string MissingPath = "/missing";

    [Fact]
    public async Task Program_MapsOpenApi_InDevelopment()
    {
        // Arrange
        await using var factory = CreateFactory(Environments.Development);
        using var client = TestHttp.CreateClient(factory, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync(TestHttp.OpenApiDocumentPath, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Program_DoesNotMapOpenApi_OutsideDevelopment()
    {
        // Arrange
        await using var factory = CreateFactory(Environments.Production);
        using var client = TestHttp.CreateClient(factory, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync(TestHttp.OpenApiDocumentPath, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Program_UsesHttpsRedirection()
    {
        // Arrange
        await using var factory = CreateFactory(
            Environments.Production,
            services => services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = 443));
        using var client = TestHttp.CreateClient(factory, TestHttp.HttpLocalhost);

        // Act
        var response = await client.GetAsync(MissingPath, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldBe($"{TestHttp.HttpsLocalhost}{MissingPath}");
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

