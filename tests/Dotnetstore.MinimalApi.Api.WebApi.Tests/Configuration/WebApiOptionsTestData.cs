using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Microsoft.AspNetCore.Http;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Configuration;

internal static class WebApiOptionsTestData
{
    internal static WebApiOptions CreateValidOptions() => new()
    {
        Cors = new WebApiCorsOptions
        {
            AllowedOrigins = ["http://localhost:7000"],
            AllowedMethods = [HttpMethods.Get, HttpMethods.Post, HttpMethods.Put]
        },
        Hsts = new WebApiHstsOptions
        {
            Enabled = true,
            Preload = true,
            IncludeSubDomains = true,
            MaxAgeDays = 30
        },
        HttpsRedirection = new WebApiHttpsRedirectionOptions
        {
            Enabled = true,
            RedirectStatusCode = StatusCodes.Status308PermanentRedirect,
            HttpsPort = 443
        },
        OpenTelemetry = new WebApiOpenTelemetryOptions
        {
            ServiceName = "webApi",
            ServiceVersion = "1.0.0",
            Tracing = new WebApiOpenTelemetryTracingOptions
            {
                Enabled = true,
                RecordException = true,
                ExcludedPaths = ["/openapi"]
            },
            Metrics = new WebApiOpenTelemetryMetricsOptions
            {
                Enabled = true
            }
        },
        RateLimiting = new WebApiRateLimitingOptions
        {
            RejectionStatusCode = StatusCodes.Status429TooManyRequests,
            RejectionMessage = "Too many requests. Please try again later.",
            PartitionKeyFallback = "unknown",
            GlobalPermitLimit = 50,
            GlobalQueueLimit = 10,
            GlobalWindowSeconds = 15,
            ShortPermitLimit = 10,
            ShortQueueLimit = 0,
            ShortWindowSeconds = 15
        }
    };
}

