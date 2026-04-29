using System.Net;
using Asp.Versioning;
using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Dotnetstore.MinimalApi.Api.WebApi.Endpoints;
using Dotnetstore.MinimalApi.Api.WebApi.Filters;
using Dotnetstore.MinimalApi.Api.WebApi.Handlers;
using Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        await using var app = CreateApp();
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
    public async Task MapEndpoints_ShouldReturnUnauthorized_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp();
        ITestEndpoints sut = new TestEndpoints(new WebApplicationHandlers());
        sut.MapEndpoints(app);
        await app.StartAsync(cancellationToken);

        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateVersionedRequest(HttpMethod.Get, TestPath, "1.0");

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MapEndpoints_ShouldReturnHelloWorld_WhenCalledWithValidApiKeyAndV1Request()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp();
        ITestEndpoints sut = new TestEndpoints(new WebApplicationHandlers());
        sut.MapEndpoints(app);
        await app.StartAsync(cancellationToken);

        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateAuthorizedVersionedRequest(HttpMethod.Get, TestPath, "1.0");

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe("Hello World!");
    }

    [Fact]
    public async Task MapEndpoints_ShouldExecuteLogPerformanceFilter_WhenHandlingV1Request()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var logger = new TestLogger<LogPerformanceFilter>();
        await using var app = CreateApp(services =>
            services.AddSingleton<ILogger<LogPerformanceFilter>>(logger));
        ITestEndpoints sut = new TestEndpoints(new WebApplicationHandlers());
        sut.MapEndpoints(app);
        await app.StartAsync(cancellationToken);

        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateAuthorizedVersionedRequest(HttpMethod.Get, TestPath, "1.0");

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        var entry = logger.Entries.ShouldHaveSingleItem();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        entry.LogLevel.ShouldBe(LogLevel.Information);
        entry.Message.ShouldContain("Endpoint execution time:");
        entry.Message.ShouldContain(" ms");
    }

    private static WebApplication CreateApp(Action<IServiceCollection>? configureServices = null) =>
        TestApplication.CreateVersionedApp(services =>
        {
            services.AddScoped<ApiKeyFilter>();
            services.AddSingleton(Options.Create(new ApiKeySecurityOptions
            {
                HeaderName = TestHttp.ApiKeyHeaderName,
                Value = TestHttp.ApiKeyValue
            }));

            configureServices?.Invoke(services);
        });

    private sealed record LogEntry(LogLevel LogLevel, string Message);

    private sealed class TestLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> _entries = [];

        internal IReadOnlyList<LogEntry> Entries => _entries;

        IDisposable? ILogger.BeginScope<TState>(TState state) where TState : default => null;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }
}

