using System.Net;
using Asp.Versioning;
using Dotnetstore.MinimalApi.Api.WebApi.Endpoints;
using Dotnetstore.MinimalApi.Api.WebApi.Handlers;
using Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;
using Dotnetstore.MinimalApi.Api.WebApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Extensions;

/// <summary>
/// Covers <c>WebApplicationExtensions</c> and verifies service registration plus middleware pipeline behavior.
/// </summary>
public sealed class WebApplicationExtensionsTests
{
    private const string AllowedOrigin = "http://localhost:7000";
    private const string DisallowedOrigin = "http://localhost:7001";
    private const string OrdersPath = "/orders";
    private const string PingPath = "/ping";
    private const string ProductionEnvironment = "Production";
    private const string TraceHeaderName = "X-Trace-Id";

    [Fact]
    public void RegisterWebApi_ReturnsSameBuilderInstance()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var result = builder.RegisterWebApi();

        // Assert
        result.ShouldBe(builder);
    }

    [Fact]
    public void RegisterWebApi_ConfiguresApiVersioningOptions()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;
        var request = new DefaultHttpContext().Request;
        request.Headers.Append(TestHttp.ApiVersionHeaderName, "1.0");

        // Assert
        options.DefaultApiVersion.ShouldBe(new ApiVersion(1, 0));
        options.ReportApiVersions.ShouldBeTrue();
        options.AssumeDefaultVersionWhenUnspecified.ShouldBeTrue();

        var versionReader = options.ApiVersionReader.ShouldBeOfType<HeaderApiVersionReader>();
        versionReader.HeaderNames.ShouldHaveSingleItem().ShouldBe(TestHttp.ApiVersionHeaderName);
        versionReader.Read(request).ShouldHaveSingleItem().ShouldBe("1.0");
    }

    [Fact]
    public void RegisterWebApi_RegistersWebApplicationHandlers_AsScopedService()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RegisterWebApi();

        var serviceDescriptor = builder.Services.SingleOrDefault(service =>
            service.ServiceType == typeof(IWebApplicationHandlers));

        using var serviceProvider = builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var scope = serviceProvider.CreateScope();

        // Assert
        serviceDescriptor.ShouldNotBeNull();
        serviceDescriptor.ImplementationType.ShouldBe(typeof(WebApplicationHandlers));
        serviceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        scope.ServiceProvider.GetRequiredService<IWebApplicationHandlers>().ShouldBeOfType<WebApplicationHandlers>();
    }

    [Fact]
    public void RegisterWebApi_RegistersTestEndpoints_AsScopedService()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RegisterWebApi();

        var serviceDescriptor = builder.Services.SingleOrDefault(service =>
            service.ServiceType == typeof(ITestEndpoints));

        using var serviceProvider = builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var scope = serviceProvider.CreateScope();

        // Assert
        serviceDescriptor.ShouldNotBeNull();
        serviceDescriptor.ImplementationType.ShouldBe(typeof(TestEndpoints));
        serviceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        scope.ServiceProvider.GetRequiredService<ITestEndpoints>().ShouldBeOfType<TestEndpoints>();
    }

    [Fact]
    public async Task RegisterMiddlewares_MapsOpenApi_InDevelopment()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Development,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync(TestHttp.OpenApiDocumentPath, cancellationToken);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterMiddlewares_DoesNotMapOpenApi_OutsideDevelopment()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync(TestHttp.OpenApiDocumentPath, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegisterMiddlewares_UsesHttpsRedirection()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureServices: services => services.AddHttpsRedirection(options => options.HttpsPort = 443),
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpLocalhost);

        // Act
        var response = await client.GetAsync(PingPath, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
        response.Headers.Location?.ToString().ShouldBe($"{TestHttp.HttpsLocalhost}{PingPath}");
    }

    [Fact]
    public async Task RegisterMiddlewares_AllowsConfiguredCorsOrigin_OnGetRequest()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateOriginRequest(HttpMethod.Get, PingPath, AllowedOrigin);

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe(AllowedOrigin);
    }

    [Fact]
    public async Task RegisterMiddlewares_AllowsConfiguredCorsOrigin_OnPreflightRequest()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapPost(OrdersPath, () => Results.Ok()));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateCorsPreflightRequest(OrdersPath, AllowedOrigin, HttpMethod.Post.Method, TraceHeaderName);

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe(AllowedOrigin);
        response.Headers.GetValues("Access-Control-Allow-Methods").Single().ShouldContain(HttpMethod.Post.Method);
        response.Headers.GetValues("Access-Control-Allow-Headers").Single().ShouldContain(TraceHeaderName);
    }

    [Fact]
    public async Task RegisterMiddlewares_DoesNotAllowDisallowedCorsMethod_OnPreflightRequest()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapDelete(OrdersPath, () => Results.Ok()));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateCorsPreflightRequest(OrdersPath, AllowedOrigin, HttpMethod.Delete.Method, TraceHeaderName);

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe(AllowedOrigin);

        var hasAllowedMethods = response.Headers.TryGetValues("Access-Control-Allow-Methods", out var allowedMethods);

        if (hasAllowedMethods)
        {
            var allowedMethodsValue = allowedMethods?.Single();

            allowedMethodsValue.ShouldNotBeNull();
            allowedMethodsValue.ShouldNotContain(HttpMethod.Delete.Method);
        }
    }

    [Fact]
    public async Task RegisterMiddlewares_DoesNotAllowUnconfiguredCorsOrigin()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateOriginRequest(HttpMethod.Get, PingPath, DisallowedOrigin);

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task RunWebApiAsync_StopsWhenCancellationIsRequested()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(
            Environments.Development,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(150));

        // Act
        var exception = await Record.ExceptionAsync(async () =>
            await app.RunWebApiAsync(cancellationTokenSource.Token));

        // Assert
        ShouldBeCancellationOrComplete(exception);
    }

    private static WebApplicationBuilder CreateBuilder(string environment = ProductionEnvironment)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment
        });

        builder.WebHost.UseTestServer();

        return builder;
    }

    private static async Task<WebApplication> CreateStartedAppAsync(
        string environment,
        CancellationToken cancellationToken,
        Action<IServiceCollection>? configureServices = null,
        Action<WebApplication>? configureRoutes = null)
    {
        var app = CreateApp(environment, configureServices, configureRoutes);

        await app.StartAsync(cancellationToken);

        return app;
    }

    private static WebApplication CreateApp(
        string environment,
        Action<IServiceCollection>? configureServices = null,
        Action<WebApplication>? configureRoutes = null)
    {
        var builder = CreateBuilder(environment);

        builder.RegisterWebApi();
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();

        app.RegisterMiddlewares();
        configureRoutes?.Invoke(app);

        return app;
    }


    private static void ShouldBeCancellationOrComplete(Exception? exception)
    {
        exception?.ShouldBeOfType<OperationCanceledException>();
    }
}
