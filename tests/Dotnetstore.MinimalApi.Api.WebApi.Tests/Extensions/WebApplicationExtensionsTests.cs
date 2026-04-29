using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Dotnetstore.MinimalApi.Api.WebApi.Filters;
using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Dotnetstore.MinimalApi.Api.WebApi.Endpoints;
using Dotnetstore.MinimalApi.Api.WebApi.Exceptions;
using Dotnetstore.MinimalApi.Api.WebApi.Handlers;
using Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;
using Dotnetstore.MinimalApi.Api.WebApi.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
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
    private const string DisallowedOrigin = "http://localhost:7001";
    private const string DevelopmentHttpsLocalhost = "https://localhost:7201";
    private const string DockerEnvironment = "Docker";
    private const string HttpsExampleCom = "https://example.com";
    private const string LimitedPath = "/limited";
    private const string OrdersPath = "/orders";
    private const string PingPath = "/ping";
    private const string ProductionEnvironment = "Production";
    private const string ScalarDocsPath = "/docs/";
    private const string TraceHeaderName = "X-Trace-Id";
    private static readonly string AllowedOrigin = WebApiDefaultValues.CorsAllowedOrigins.Single();
    private static readonly string TooManyRequestsMessage = new WebApiRateLimitingOptions().RejectionMessage;

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
    public void RegisterWebApi_RegistersWebApplicationHandlers_AsSingletonService()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RegisterWebApi();

        var serviceDescriptor = builder.Services.SingleOrDefault(service =>
            service.ServiceType == typeof(IWebApplicationHandlers));

        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        serviceDescriptor.ShouldNotBeNull();
        serviceDescriptor.ImplementationType.ShouldBe(typeof(WebApplicationHandlers));
        serviceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        serviceProvider.GetRequiredService<IWebApplicationHandlers>().ShouldBeOfType<WebApplicationHandlers>();
    }

    [Fact]
    public void RegisterWebApi_RegistersTestEndpoints_AsSingletonService()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RegisterWebApi();

        var serviceDescriptor = builder.Services.SingleOrDefault(service =>
            service.ServiceType == typeof(ITestEndpoints));

        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        serviceDescriptor.ShouldNotBeNull();
        serviceDescriptor.ImplementationType.ShouldBe(typeof(TestEndpoints));
        serviceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        serviceProvider.GetRequiredService<ITestEndpoints>().ShouldBeOfType<TestEndpoints>();
    }

    [Fact]
    public void RegisterWebApi_RegistersExceptionHandlingDependencies()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RegisterWebApi();

        var serviceDescriptor = builder.Services.SingleOrDefault(service =>
            service.ServiceType == typeof(IExceptionHandler)
            && service.ImplementationType == typeof(DefaultExceptionHandler));

        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        serviceDescriptor.ShouldNotBeNull();
        serviceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        serviceProvider.GetRequiredService<IProblemDetailsService>().ShouldNotBeNull();
    }

    [Fact]
    public void RegisterWebApi_RegistersApiKeyFilter_AsScopedService()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RegisterWebApi();

        var serviceDescriptor = builder.Services.LastOrDefault(service =>
            service.ServiceType == typeof(ApiKeyFilter));

        using var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        // Assert
        serviceDescriptor.ShouldNotBeNull();
        serviceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        scope.ServiceProvider.GetRequiredService<ApiKeyFilter>().ShouldNotBeNull();
    }

    [Fact]
    public void RegisterWebApi_ShouldBindApiKeyOptions_FromAppsettingsJson()
    {
        // Arrange
        var builder = CreateBuilder(Environments.Production);

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ApiKeySecurityOptions>>().Value;

        // Assert
        options.HeaderName.ShouldBe(TestHttp.ApiKeyHeaderName);
        options.Value.ShouldBe(TestHttp.ApiKeyValue);
    }

    [Fact]
    public void RegisterWebApi_ShouldConfigureHstsOptions_WhenCalled()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HstsOptions>>().Value;

        // Assert
        options.Preload.ShouldBeTrue();
        options.IncludeSubDomains.ShouldBeTrue();
        options.MaxAge.ShouldBe(TimeSpan.FromDays(30));
    }

    [Fact]
    public void RegisterWebApi_ShouldBindProductionWebApiOptions_FromAppsettingsJson()
    {
        // Arrange
        var builder = CreateBuilder(Environments.Production);

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<WebApiOptions>>().Value;

        // Assert
        options.Cors.AllowedOrigins.ShouldBe(["http://localhost:7000"]);
        options.Cors.AllowedMethods.ShouldBe([HttpMethods.Get, HttpMethods.Post, HttpMethods.Put]);
        options.HttpsRedirection.RedirectStatusCode.ShouldBe(StatusCodes.Status308PermanentRedirect);
        options.HttpsRedirection.HttpsPort.ShouldBe(443);
    }

    [Fact]
    public void RegisterWebApi_ShouldBindDevelopmentWebApiOptions_FromAppsettingsDevelopmentJson()
    {
        // Arrange
        var builder = CreateBuilder(Environments.Development);

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<WebApiOptions>>().Value;

        // Assert
        options.Cors.AllowedOrigins.ShouldBe(["http://localhost:7000"]);
        options.Cors.AllowedMethods.ShouldBe([HttpMethods.Get, HttpMethods.Post, HttpMethods.Put]);
        options.HttpsRedirection.RedirectStatusCode.ShouldBe(StatusCodes.Status307TemporaryRedirect);
        options.HttpsRedirection.HttpsPort.ShouldBe(7201);
    }

    [Fact]
    public void RegisterWebApi_ShouldBindDockerWebApiOptions_FromAppsettingsDockerJson()
    {
        // Arrange
        var builder = CreateBuilder(DockerEnvironment);

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<WebApiOptions>>().Value;

        // Assert
        options.Hsts.Enabled.ShouldBeFalse();
        options.HttpsRedirection.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void RegisterWebApi_ShouldConfigureHttpsRedirection_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        var builder = CreateBuilder(Environments.Development);

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HttpsRedirectionOptions>>().Value;

        // Assert
        options.RedirectStatusCode.ShouldBe(StatusCodes.Status307TemporaryRedirect);
        options.HttpsPort.ShouldBe(7201);
    }

    [Fact]
    public void RegisterWebApi_ShouldConfigureHttpsRedirection_WhenEnvironmentIsProduction()
    {
        // Arrange
        var builder = CreateBuilder(Environments.Production);

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HttpsRedirectionOptions>>().Value;

        // Assert
        options.RedirectStatusCode.ShouldBe(StatusCodes.Status308PermanentRedirect);
        options.HttpsPort.ShouldBe(443);
    }

    [Fact]
    public void RegisterWebApi_ShouldConfigureRateLimiterOptions_WhenCalled()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        // Assert
        options.RejectionStatusCode.ShouldBe((int)HttpStatusCode.TooManyRequests);
        options.OnRejected.ShouldNotBeNull();
        options.GlobalLimiter.ShouldNotBeNull();
    }


    [Fact]
    public void RegisterWebApi_ShouldBindCorsAndRateLimitingOptions_FromConfiguration()
    {
        // Arrange
        const string configuredOrigin = "http://localhost:7100";
        const string configuredMethod = "DELETE";
        const string configuredMessage = "Custom throttling message.";
        const string configuredPartitionFallback = "anonymous-client";
        var builder = CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{WebApiConfiguration.OptionsSectionName}:Cors:AllowedOrigins:0"] = configuredOrigin,
            [$"{WebApiConfiguration.OptionsSectionName}:Cors:AllowedMethods:0"] = configuredMethod,
            [$"{WebApiConfiguration.OptionsSectionName}:RateLimiting:RejectionMessage"] = configuredMessage,
            [$"{WebApiConfiguration.OptionsSectionName}:RateLimiting:PartitionKeyFallback"] = configuredPartitionFallback
        });

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var webApiOptions = serviceProvider.GetRequiredService<IOptions<WebApiOptions>>().Value;
        var allowedMethods = webApiOptions.Cors.AllowedMethods.ShouldNotBeNull();

        // Assert
        webApiOptions.Cors.AllowedOrigins.ShouldHaveSingleItem().ShouldBe(configuredOrigin);
        allowedMethods.ShouldContain(configuredMethod);
        webApiOptions.RateLimiting.RejectionMessage.ShouldBe(configuredMessage);
        webApiOptions.RateLimiting.PartitionKeyFallback.ShouldBe(configuredPartitionFallback);
    }

    [Fact]
    public void RegisterWebApi_ShouldBindOpenTelemetryOptions_FromConfiguration()
    {
        // Arrange
        const string configuredServiceName = "webApi-tests";
        const string configuredServiceVersion = "2.5.0";
        var builder = CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{WebApiConfiguration.OptionsSectionName}:OpenTelemetry:ServiceName"] = configuredServiceName,
            [$"{WebApiConfiguration.OptionsSectionName}:OpenTelemetry:ServiceVersion"] = configuredServiceVersion,
            [$"{WebApiConfiguration.OptionsSectionName}:OpenTelemetry:Tracing:ExcludedPaths:0"] = "/health"
        });

        // Act
        builder.RegisterWebApi();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var webApiOptions = serviceProvider.GetRequiredService<IOptions<WebApiOptions>>().Value;

        // Assert
        webApiOptions.OpenTelemetry.ServiceName.ShouldBe(configuredServiceName);
        webApiOptions.OpenTelemetry.ServiceVersion.ShouldBe(configuredServiceVersion);
        webApiOptions.OpenTelemetry.Tracing.ExcludedPaths.ShouldBe(["/health"]);
        webApiOptions.OpenTelemetry.Metrics.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterWebApi_ShouldFailStartup_WhenCorsAllowedOriginsIsEmpty()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Should.ThrowAsync<OptionsValidationException>(async () =>
        {
            await using var app = CreateApp(
                Environments.Production,
                configureConfiguration: configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{WebApiConfiguration.OptionsSectionName}:Cors:AllowedOrigins:0"] = " "
                }),
                configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));

            await app.StartAsync(cancellationToken);
        });

        // Assert
        exception.Message.ShouldContain("WebApi:Cors:AllowedOrigins");
    }

    [Fact]
    public async Task RegisterWebApi_ShouldFailStartup_WhenRateLimiterRejectionMessageIsBlank()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Should.ThrowAsync<OptionsValidationException>(async () =>
        {
            await using var app = CreateApp(
                Environments.Production,
                configureConfiguration: configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{WebApiConfiguration.OptionsSectionName}:RateLimiting:RejectionMessage"] = "   "
                }),
                configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));

            await app.StartAsync(cancellationToken);
        });

        // Assert
        exception.Message.ShouldContain("WebApi:RateLimiting:RejectionMessage");
    }

    [Fact]
    public async Task RegisterWebApi_ShouldFailStartup_WhenRateLimiterPermitLimitIsNotPositive()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Should.ThrowAsync<OptionsValidationException>(async () =>
        {
            await using var app = CreateApp(
                Environments.Production,
                configureConfiguration: configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{WebApiConfiguration.OptionsSectionName}:RateLimiting:GlobalPermitLimit"] = "0"
                }),
                configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));

            await app.StartAsync(cancellationToken);
        });

        // Assert
        exception.Message.ShouldContain("WebApi:RateLimiting:GlobalPermitLimit");
    }

    [Fact]
    public async Task RegisterWebApi_ShouldFailStartup_WhenOpenTelemetryServiceNameIsBlank()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Should.ThrowAsync<OptionsValidationException>(async () =>
        {
            await using var app = CreateApp(
                Environments.Production,
                configureConfiguration: configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{WebApiConfiguration.OptionsSectionName}:OpenTelemetry:ServiceName"] = " "
                }),
                configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));

            await app.StartAsync(cancellationToken);
        });

        // Assert
        exception.Message.ShouldContain("WebApi:OpenTelemetry:ServiceName");
    }

    [Fact]
    public async Task RegisterWebApi_ShouldFailStartup_WhenApiKeyHeaderNameIsBlank()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Should.ThrowAsync<OptionsValidationException>(async () =>
        {
            await using var app = CreateApp(
                Environments.Production,
                configureConfiguration: configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{ApiKeySecurityOptions.SectionName}:HeaderName"] = " ",
                    [$"{ApiKeySecurityOptions.SectionName}:Value"] = "some-value"
                }),
                configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));

            await app.StartAsync(cancellationToken);
        });

        // Assert
        exception.Message.ShouldContain("ApiKey:HeaderName");
    }

    [Fact]
    public async Task RegisterWebApi_ShouldFailStartup_WhenApiKeyValueIsBlank()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Should.ThrowAsync<OptionsValidationException>(async () =>
        {
            await using var app = CreateApp(
                Environments.Production,
                configureConfiguration: configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{ApiKeySecurityOptions.SectionName}:HeaderName"] = "X-API-KEY",
                    [$"{ApiKeySecurityOptions.SectionName}:Value"] = " "
                }),
                configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));

            await app.StartAsync(cancellationToken);
        });

        // Assert
        exception.Message.ShouldContain("ApiKey:Value");
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldAllowConfiguredCorsOrigin_FromConfiguration()
    {
        // Arrange
        const string configuredOrigin = "http://localhost:7100";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureConfiguration: configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{WebApiConfiguration.OptionsSectionName}:Cors:AllowedOrigins:0"] = configuredOrigin
            }),
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var configuredOriginRequest = TestHttp.CreateOriginRequest(HttpMethod.Get, PingPath, configuredOrigin);
        using var defaultOriginRequest = TestHttp.CreateOriginRequest(HttpMethod.Get, PingPath, AllowedOrigin);

        // Act
        var configuredOriginResponse = await client.SendAsync(configuredOriginRequest, cancellationToken);
        var defaultOriginResponse = await client.SendAsync(defaultOriginRequest, cancellationToken);

        // Assert
        configuredOriginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        configuredOriginResponse.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe(configuredOrigin);
        defaultOriginResponse.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldAllowConfiguredCorsMethod_FromConfiguration_OnPreflightRequest()
    {
        // Arrange
        const string configuredOrigin = "http://localhost:7100";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureConfiguration: configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{WebApiConfiguration.OptionsSectionName}:Cors:AllowedOrigins:0"] = configuredOrigin,
                [$"{WebApiConfiguration.OptionsSectionName}:Cors:AllowedMethods:0"] = HttpMethod.Delete.Method
            }),
            configureRoutes: webApplication => webApplication.MapDelete(OrdersPath, () => Results.Ok()));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);
        using var request = TestHttp.CreateCorsPreflightRequest(OrdersPath, configuredOrigin, HttpMethod.Delete.Method, TraceHeaderName);

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe(configuredOrigin);
        response.Headers.GetValues("Access-Control-Allow-Methods").Single().ShouldContain(HttpMethod.Delete.Method);
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
    public async Task RegisterMiddlewares_MapsScalarApiReference_InDevelopment()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Development,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync(ScalarDocsPath, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<title>Dotnetstore MinimalApi Web API</title>");
        content.ShouldContain("scalar");
    }

    [Fact]
    public async Task RegisterMiddlewares_DoesNotMapScalarApiReference_OutsideDevelopment()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync(ScalarDocsPath, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldDescribeApiKeySecurityScheme_InOpenApiDocument()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Development,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        using var document = await GetOpenApiDocumentAsync(client, cancellationToken);
        var apiKeyScheme = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("ApiKey");

        // Assert
        apiKeyScheme.GetProperty("type").GetString().ShouldBe("apiKey");
        apiKeyScheme.GetProperty("name").GetString().ShouldBe(TestHttp.ApiKeyHeaderName);
        apiKeyScheme.GetProperty("in").GetString().ShouldBe("header");
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldApplyApiKeySecurityRequirement_ToAllMappedOperationsInOpenApiDocument()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Development,
            cancellationToken,
            configureRoutes: webApplication =>
            {
                webApplication.MapGet(PingPath, () => Results.Ok("pong")).WithName("Ping");
                webApplication.MapPost(OrdersPath, () => Results.Ok()).WithName("CreateOrder");
            });
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        using var document = await GetOpenApiDocumentAsync(client, cancellationToken);
        var paths = document.RootElement.GetProperty("paths");

        // Assert
        ShouldContainApiKeySecurityRequirement(paths.GetProperty(PingPath).GetProperty("get"));
        ShouldContainApiKeySecurityRequirement(paths.GetProperty(OrdersPath).GetProperty("post"));
    }

    [Fact]
    public async Task RegisterMiddlewares_UsesHttpsRedirection()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpLocalhost);

        // Act
        var response = await client.GetAsync(PingPath, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PermanentRedirect);
        response.Headers.Location?.ToString().ShouldBe($"{TestHttp.HttpsLocalhost}{PingPath}");
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldUseDevelopmentHttpsRedirection_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Development,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpLocalhost);

        // Act
        var response = await client.GetAsync(PingPath, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
        response.Headers.Location?.ToString().ShouldBe($"{DevelopmentHttpsLocalhost}{PingPath}");
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldNotUseHttpsRedirection_WhenEnvironmentIsDocker()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            DockerEnvironment,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpLocalhost);

        // Act
        var response = await client.GetAsync(PingPath, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe("\"pong\"");
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldAddHstsHeader_WhenEnvironmentIsProduction()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, HttpsExampleCom);

        // Act
        var response = await client.GetAsync(PingPath, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Strict-Transport-Security").Single().ShouldBe("max-age=2592000; includeSubDomains; preload");
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldNotAddHstsHeader_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Development,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync(PingPath, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Strict-Transport-Security").ShouldBeFalse();
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
    public async Task RegisterMiddlewares_ShouldRejectRequests_WhenShortLimitIsExceeded()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication
                .MapGet(LimitedPath, async (CancellationToken requestCancellationToken) =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), requestCancellationToken);
                    return Results.Ok("pong");
                })
                .RequireRateLimiting(WebApiConfiguration.ShortRateLimitPolicyName));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var requests = Enumerable.Range(0, 11)
            .Select(_ => client.GetAsync(LimitedPath, cancellationToken))
            .ToArray();
        var responses = await Task.WhenAll(requests);

        var rejectedResponses = responses
            .Where(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .ToList();
        var successfulResponses = responses
            .Where(response => response.StatusCode == HttpStatusCode.OK)
            .ToList();

        // Assert
        rejectedResponses.Count.ShouldBe(1);
        successfulResponses.Count.ShouldBe(10);

        foreach (var rejectedResponse in rejectedResponses)
        {
            (await rejectedResponse.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(TooManyRequestsMessage);
        }
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldUseConfiguredRateLimiterRejectionMessage_WhenRequestIsRejected()
    {
        // Arrange
        const string configuredMessage = "Please wait before trying again.";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureConfiguration: configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{WebApiConfiguration.OptionsSectionName}:RateLimiting:ShortPermitLimit"] = "1",
                [$"{WebApiConfiguration.OptionsSectionName}:RateLimiting:ShortQueueLimit"] = "0",
                [$"{WebApiConfiguration.OptionsSectionName}:RateLimiting:ShortWindowSeconds"] = "60",
                [$"{WebApiConfiguration.OptionsSectionName}:RateLimiting:RejectionMessage"] = configuredMessage,
                [$"{WebApiConfiguration.OptionsSectionName}:RateLimiting:PartitionKeyFallback"] = "configured-fallback"
            }),
            configureRoutes: webApplication => webApplication
                .MapGet(LimitedPath, async (CancellationToken requestCancellationToken) =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), requestCancellationToken);
                    return Results.Ok("pong");
                })
                .RequireRateLimiting(WebApiConfiguration.ShortRateLimitPolicyName));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var responses = await Task.WhenAll(
            client.GetAsync(LimitedPath, cancellationToken),
            client.GetAsync(LimitedPath, cancellationToken));

        var rejectedResponse = responses.Single(response => response.StatusCode == HttpStatusCode.TooManyRequests);

        // Assert
        (await rejectedResponse.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(configuredMessage);
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldRejectRequestsUsingGlobalLimiter_WhenEndpointHasNoExplicitPolicy()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureServices: services => services.PostConfigure<RateLimiterOptions>(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 2,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            }),
            configureRoutes: webApplication => webApplication.MapGet(PingPath, () => Results.Ok("pong")));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var firstResponse = await client.GetAsync(PingPath, cancellationToken);
        var secondResponse = await client.GetAsync(PingPath, cancellationToken);
        var thirdResponse = await client.GetAsync(PingPath, cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        thirdResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await thirdResponse.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(TooManyRequestsMessage);
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldReturnBadRequestProblemDetails_WhenValidationExceptionIsThrown()
    {
        // Arrange
        const string errorMessage = "Name is required.";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet("/throw-validation",
                IResult () => throw new ValidationException(errorMessage)));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync("/throw-validation", cancellationToken);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problemDetails.ShouldNotBeNull();
        problemDetails.Type.ShouldBe("bad-request");
        problemDetails.Title.ShouldBe("The request is invalid.");
        problemDetails.Detail.ShouldBe(errorMessage);
        problemDetails.Status.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldReturnNotFoundProblemDetails_WhenKeyNotFoundExceptionIsThrown()
    {
        // Arrange
        const string errorMessage = "Resource not found.";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet("/throw-not-found",
                IResult () => throw new KeyNotFoundException(errorMessage)));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync("/throw-not-found", cancellationToken);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problemDetails.ShouldNotBeNull();
        problemDetails.Type.ShouldBe("not-found");
        problemDetails.Title.ShouldBe("The requested resource was not found.");
        problemDetails.Detail.ShouldBe(errorMessage);
        problemDetails.Status.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldReturnUnauthorizedProblemDetails_WhenUnauthorizedAccessExceptionIsThrown()
    {
        // Arrange
        const string errorMessage = "Access denied.";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet("/throw-unauthorized",
                IResult () => throw new UnauthorizedAccessException(errorMessage)));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync("/throw-unauthorized", cancellationToken);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problemDetails.ShouldNotBeNull();
        problemDetails.Type.ShouldBe("unauthorized");
        problemDetails.Title.ShouldBe("Authentication is required.");
        problemDetails.Detail.ShouldBe(errorMessage);
        problemDetails.Status.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldReturnInternalServerErrorProblemDetails_WhenUnexpectedExceptionIsThrown()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet("/throw-unexpected",
                IResult () => throw new InvalidOperationException("Sensitive internal details.")));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync("/throw-unexpected", cancellationToken);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        problemDetails.ShouldNotBeNull();
        problemDetails.Type.ShouldBe("internal-server-error");
        problemDetails.Title.ShouldBe("An unexpected error occurred while processing your request.");
        problemDetails.Detail.ShouldBe("An unexpected error occurred.");
        problemDetails.Status.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task RegisterMiddlewares_ShouldIncludeInstanceInProblemDetails_WhenExceptionIsThrown()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateStartedAppAsync(
            Environments.Production,
            cancellationToken,
            configureRoutes: webApplication => webApplication.MapGet("/throw-instance",
                IResult () => throw new KeyNotFoundException("Not found.")));
        using var client = TestHttp.CreateClient(app, TestHttp.HttpsLocalhost);

        // Act
        var response = await client.GetAsync("/throw-instance", cancellationToken);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        // Assert
        problemDetails.ShouldNotBeNull();
        problemDetails.Instance.ShouldBe("GET /throw-instance");
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
        Action<ConfigurationManager>? configureConfiguration = null,
        Action<IServiceCollection>? configureServices = null,
        Action<WebApplication>? configureRoutes = null)
    {
        var app = CreateApp(environment, configureConfiguration, configureServices, configureRoutes);

        await app.StartAsync(cancellationToken);

        return app;
    }

    private static WebApplication CreateApp(
        string environment,
        Action<ConfigurationManager>? configureConfiguration = null,
        Action<IServiceCollection>? configureServices = null,
        Action<WebApplication>? configureRoutes = null)
    {
        var builder = CreateBuilder(environment);

        configureConfiguration?.Invoke(builder.Configuration);
        builder.RegisterWebApi();
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();

        app.RegisterMiddlewares();
        configureRoutes?.Invoke(app);

        return app;
    }

    private static async Task<JsonDocument> GetOpenApiDocumentAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(TestHttp.OpenApiDocumentPath, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
    }

    private static void ShouldContainApiKeySecurityRequirement(JsonElement operation)
    {
        var security = operation.GetProperty("security");
        security.ValueKind.ShouldBe(JsonValueKind.Array);

        security
            .EnumerateArray()
            .Any(requirement => requirement.TryGetProperty("ApiKey", out var scopes) && scopes.ValueKind == JsonValueKind.Array && scopes.GetArrayLength() == 0)
            .ShouldBeTrue();
    }


    private static void ShouldBeCancellationOrComplete(Exception? exception)
    {
        exception?.ShouldBeOfType<OperationCanceledException>();
    }
}
