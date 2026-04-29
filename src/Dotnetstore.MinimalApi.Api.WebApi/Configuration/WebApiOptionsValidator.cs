using Microsoft.Extensions.Options;

namespace Dotnetstore.MinimalApi.Api.WebApi.Configuration;

internal sealed class WebApiOptionsValidator : IValidateOptions<WebApiOptions>
{
    public ValidateOptionsResult Validate(string? name, WebApiOptions options)
    {
        var errors = new List<string>();

        ValidateStringArray(options.Cors.AllowedOrigins, "WebApi:Cors:AllowedOrigins", errors);
        ValidateStringArray(options.Cors.AllowedMethods, "WebApi:Cors:AllowedMethods", errors);

        if (options.Hsts.Enabled && options.Hsts.MaxAgeDays <= 0)
        {
            errors.Add("WebApi:Hsts:MaxAgeDays must be greater than 0.");
        }

        if (options.HttpsRedirection.Enabled)
        {
            ValidateHttpsRedirectionOptions(options.HttpsRedirection, "WebApi:HttpsRedirection", errors);
        }
        ValidateOpenTelemetryOptions(options.OpenTelemetry, errors);

        if (options.RateLimiting.RejectionStatusCode < StatusCodes.Status400BadRequest
            || options.RateLimiting.RejectionStatusCode > 599)
        {
            errors.Add("WebApi:RateLimiting:RejectionStatusCode must be between 400 and 599.");
        }

        ValidateRequiredString(options.RateLimiting.RejectionMessage, "WebApi:RateLimiting:RejectionMessage", errors);
        ValidateRequiredString(options.RateLimiting.PartitionKeyFallback, "WebApi:RateLimiting:PartitionKeyFallback", errors);
        ValidatePositiveInteger(options.RateLimiting.GlobalPermitLimit, "WebApi:RateLimiting:GlobalPermitLimit", errors);
        ValidateNonNegativeInteger(options.RateLimiting.GlobalQueueLimit, "WebApi:RateLimiting:GlobalQueueLimit", errors);
        ValidatePositiveInteger(options.RateLimiting.GlobalWindowSeconds, "WebApi:RateLimiting:GlobalWindowSeconds", errors);
        ValidatePositiveInteger(options.RateLimiting.ShortPermitLimit, "WebApi:RateLimiting:ShortPermitLimit", errors);
        ValidateNonNegativeInteger(options.RateLimiting.ShortQueueLimit, "WebApi:RateLimiting:ShortQueueLimit", errors);
        ValidatePositiveInteger(options.RateLimiting.ShortWindowSeconds, "WebApi:RateLimiting:ShortWindowSeconds", errors);

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateHttpsRedirectionOptions(
        WebApiHttpsRedirectionOptions options,
        string path,
        ICollection<string> errors)
    {
        if (options.RedirectStatusCode < 300 || options.RedirectStatusCode > 399)
        {
            errors.Add($"{path}:RedirectStatusCode must be a valid redirect status code between 300 and 399.");
        }

        if (options.HttpsPort <= 0 || options.HttpsPort > 65535)
        {
            errors.Add($"{path}:HttpsPort must be between 1 and 65535.");
        }
    }

    private static void ValidateStringArray(string[]? values, string path, ICollection<string> errors)
    {
        if (values is not { Length: > 0 })
        {
            errors.Add($"{path} must contain at least one value.");
            return;
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"{path} cannot contain empty values.");
        }
    }

    private static void ValidateOpenTelemetryOptions(WebApiOpenTelemetryOptions options, ICollection<string> errors)
    {
        ValidateRequiredString(options.ServiceName, "WebApi:OpenTelemetry:ServiceName", errors);

        if (options.ServiceVersion is not null && string.IsNullOrWhiteSpace(options.ServiceVersion))
        {
            errors.Add("WebApi:OpenTelemetry:ServiceVersion cannot be empty when provided.");
        }


        if (options.Tracing.ExcludedPaths?.Any(string.IsNullOrWhiteSpace) == true)
        {
            errors.Add("WebApi:OpenTelemetry:Tracing:ExcludedPaths cannot contain empty values.");
        }
    }

    private static void ValidateRequiredString(string? value, string path, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{path} cannot be empty.");
        }
    }

    private static void ValidatePositiveInteger(int value, string path, ICollection<string> errors)
    {
        if (value <= 0)
        {
            errors.Add($"{path} must be greater than 0.");
        }
    }

    private static void ValidateNonNegativeInteger(int value, string path, ICollection<string> errors)
    {
        if (value < 0)
        {
            errors.Add($"{path} cannot be negative.");
        }
    }
}

