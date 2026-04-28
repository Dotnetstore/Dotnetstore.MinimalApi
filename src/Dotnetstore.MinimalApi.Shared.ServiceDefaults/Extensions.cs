using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string OpenTelemetrySectionName = "WebApi:OpenTelemetry";

    private sealed class ServiceDefaultsMarker;

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(ServiceDefaultsMarker)))
        {
            return builder;
        }

        builder.Services.TryAddSingleton<ServiceDefaultsMarker>();

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var openTelemetrySection = builder.Configuration.GetSection(OpenTelemetrySectionName);
        var serviceName = openTelemetrySection["ServiceName"];
        var serviceVersion = openTelemetrySection["ServiceVersion"];
        var tracingEnabled = openTelemetrySection.GetValue<bool?>("Tracing:Enabled") ?? true;
        var metricsEnabled = openTelemetrySection.GetValue<bool?>("Metrics:Enabled") ?? true;
        var recordException = openTelemetrySection.GetValue<bool?>("Tracing:RecordException") ?? true;
        var excludedPaths = openTelemetrySection
            .GetSection("Tracing:ExcludedPaths")
            .Get<string[]>()?
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .ToArray()
            ?? Array.Empty<string>();

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var openTelemetryBuilder = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: ResolveServiceName(builder, serviceName),
                serviceVersion: ResolveServiceVersion(serviceVersion)));

        if (metricsEnabled)
        {
            openTelemetryBuilder.WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            });
        }

        if (tracingEnabled)
        {
            openTelemetryBuilder.WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = recordException;
                        // Exclude health check requests from tracing
                        options.Filter = context => ShouldTraceRequest(context, excludedPaths);
                    })
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });
        }

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static string ResolveServiceName(IHostApplicationBuilder builder, string? configuredServiceName) =>
        string.IsNullOrWhiteSpace(configuredServiceName)
            ? builder.Environment.ApplicationName
            : configuredServiceName;

    private static string ResolveServiceVersion(string? configuredServiceVersion) =>
        string.IsNullOrWhiteSpace(configuredServiceVersion)
            ? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0"
            : configuredServiceVersion;

    private static bool ShouldTraceRequest(HttpContext context, IReadOnlyCollection<string> excludedPaths) =>
        !context.Request.Path.StartsWithSegments(HealthEndpointPath)
        && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
        && excludedPaths.All(path => !context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase));

    private static void AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}