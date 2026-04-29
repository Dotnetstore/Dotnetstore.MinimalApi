namespace Dotnetstore.MinimalApi.Api.WebApi.Configuration;

internal static class WebApiDefaultValues
{
    internal static readonly string[] CorsAllowedOrigins = ["http://localhost:7000"];
    internal static readonly string[] CorsAllowedMethods = [HttpMethods.Get, HttpMethods.Post, HttpMethods.Put];
    internal static readonly string[] OpenTelemetryExcludedPaths = ["/openapi"];
}

internal sealed class WebApiOptions
{
    public WebApiCorsOptions Cors { get; init; } = new();

    public WebApiHstsOptions Hsts { get; init; } = new();

    public WebApiHttpsRedirectionOptions HttpsRedirection { get; init; } = new();

    public WebApiOpenTelemetryOptions OpenTelemetry { get; init; } = new();

    public WebApiRateLimitingOptions RateLimiting { get; init; } = new();

    internal WebApiOptions ApplyDefaults()
    {
        Cors.AllowedOrigins ??= WebApiDefaultValues.CorsAllowedOrigins;
        Cors.AllowedMethods ??= WebApiDefaultValues.CorsAllowedMethods;
        OpenTelemetry.Tracing.ExcludedPaths ??= WebApiDefaultValues.OpenTelemetryExcludedPaths;

        return this;
    }
}

internal sealed class WebApiCorsOptions
{
    public string[]? AllowedOrigins { get; set; }

    public string[]? AllowedMethods { get; set; }
}

internal sealed class WebApiHstsOptions
{
    public bool Enabled { get; set; } = true;

    public bool Preload { get; init; } = true;

    public bool IncludeSubDomains { get; init; } = true;

    public int MaxAgeDays { get; set; } = 30;
}

internal sealed class WebApiHttpsRedirectionOptions
{
    public bool Enabled { get; set; } = true;

    public int RedirectStatusCode { get; set; } = StatusCodes.Status308PermanentRedirect;

    public int HttpsPort { get; set; } = 443;
}

internal sealed class WebApiOpenTelemetryOptions
{
    public string ServiceName { get; set; } = "webApi";

    public string? ServiceVersion { get; set; }

    public WebApiOpenTelemetryTracingOptions Tracing { get; init; } = new();

    public WebApiOpenTelemetryMetricsOptions Metrics { get; init; } = new();
}


internal sealed class WebApiOpenTelemetryTracingOptions
{
    public bool Enabled { get; set; } = true;

    public bool RecordException { get; set; } = true;

    public string[]? ExcludedPaths { get; set; }
}

internal sealed class WebApiOpenTelemetryMetricsOptions
{
    public bool Enabled { get; set; } = true;
}

internal sealed class WebApiRateLimitingOptions
{
    public int RejectionStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;

    public string RejectionMessage { get; set; } = "Too many requests. Please try again later.";

    public string PartitionKeyFallback { get; set; } = "unknown";

    public int GlobalPermitLimit { get; set; } = 50;

    public int GlobalQueueLimit { get; set; } = 10;

    public int GlobalWindowSeconds { get; set; } = 15;

    public int ShortPermitLimit { get; set; } = 10;

    public int ShortQueueLimit { get; set; }

    public int ShortWindowSeconds { get; set; } = 15;
}

