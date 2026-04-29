namespace Dotnetstore.MinimalApi.Api.WebApi.Configuration;

internal sealed class ApiKeySecurityOptions
{
    internal const string SectionName = "ApiKey";

    public string HeaderName { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}