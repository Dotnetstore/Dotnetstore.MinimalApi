using Microsoft.Extensions.Options;

namespace Dotnetstore.MinimalApi.Api.WebApi.Configuration;

internal sealed class ApiKeySecurityOptionsValidator : IValidateOptions<ApiKeySecurityOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiKeySecurityOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.HeaderName))
        {
            errors.Add("ApiKey:HeaderName cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.Value))
        {
            errors.Add("ApiKey:Value cannot be empty.");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
