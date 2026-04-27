using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Configuration;

/// <summary>
/// Covers <c>WebApiConfiguration</c> and verifies the shared configuration constant values used across startup and tests.
/// </summary>
public sealed class WebApiConfigurationTests
{
    [Theory]
    [MemberData(nameof(ConfigurationConstants))]
    public void ConfigurationConstant_ShouldMatchExpectedValue_WhenReferenced(
        string actualValue,
        string expectedValue)
    {
        // Assert
        actualValue.ShouldBe(expectedValue);
    }

    public static TheoryData<string, string> ConfigurationConstants() => new()
    {
        { WebApiConfiguration.OptionsSectionName, "WebApi" },
        { WebApiConfiguration.ApiVersionHeaderName, "api-version" },
        { WebApiConfiguration.CorsPolicyName, "AllowDotnetstoreSpecificOrigins" },
        { WebApiConfiguration.ShortRateLimitPolicyName, "ShortLimit" }
    };
}

