using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Extensions;

/// <summary>
/// Covers Aspire <c>ServiceDefaults</c> registration and verifies shared telemetry plus default endpoint behavior.
/// </summary>
public sealed class ServiceDefaultsExtensionsTests
{
    [Fact]
    public void AddServiceDefaults_ShouldRegisterOpenTelemetryProviders_WhenCalled()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddServiceDefaults();

        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        serviceProvider.GetRequiredService<TracerProvider>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<MeterProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void MapDefaultEndpoints_ShouldReturnSameApplicationInstance_WhenCalled()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddServiceDefaults();
        var app = builder.Build();

        // Act
        var result = app.MapDefaultEndpoints();

        // Assert
        result.ShouldBe(app);
    }

    [Fact]
    public void AddServiceDefaults_ShouldRemainIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddServiceDefaults();
        builder.AddServiceDefaults();

        var app = builder.Build();
        app.MapDefaultEndpoints();

        // Assert
        Should.NotThrow(() => app.Services.GetRequiredService<HealthCheckService>());
        app.Services.GetRequiredService<TracerProvider>().ShouldNotBeNull();
        app.Services.GetRequiredService<MeterProvider>().ShouldNotBeNull();
    }

    private static WebApplicationBuilder CreateBuilder() =>
        WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
}
