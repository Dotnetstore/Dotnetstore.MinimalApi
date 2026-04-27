namespace Dotnetstore.MinimalApi.Api.WebApi.Configuration;

internal static class WebApiDefaultValues
{
    internal static readonly string[] CorsAllowedOrigins = ["http://localhost:7000"];
    internal static readonly string[] CorsAllowedMethods = [HttpMethods.Get, HttpMethods.Post, HttpMethods.Put];
}

internal sealed class WebApiOptions
{
    public WebApiCorsOptions Cors { get; set; } = new();

    public WebApiHstsOptions Hsts { get; set; } = new();

    public WebApiHttpsRedirectionOptions HttpsRedirection { get; set; } = new();

    public WebApiRateLimitingOptions RateLimiting { get; set; } = new();

    internal WebApiOptions ApplyDefaults()
    {
        Cors.AllowedOrigins ??= WebApiDefaultValues.CorsAllowedOrigins;
        Cors.AllowedMethods ??= WebApiDefaultValues.CorsAllowedMethods;

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
    public bool Preload { get; set; } = true;

    public bool IncludeSubDomains { get; set; } = true;

    public int MaxAgeDays { get; set; } = 30;
}

internal sealed class WebApiHttpsRedirectionOptions
{
    public WebApiEnvironmentHttpsRedirectionOptions Development { get; set; } = new()
    {
        RedirectStatusCode = StatusCodes.Status307TemporaryRedirect,
        HttpsPort = 7201
    };

    public WebApiEnvironmentHttpsRedirectionOptions Production { get; set; } = new()
    {
        RedirectStatusCode = StatusCodes.Status308PermanentRedirect,
        HttpsPort = 443
    };
}

internal sealed class WebApiEnvironmentHttpsRedirectionOptions
{
    public int RedirectStatusCode { get; set; }

    public int HttpsPort { get; set; }
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

    public int ShortQueueLimit { get; set; } = 0;

    public int ShortWindowSeconds { get; set; } = 15;
}

