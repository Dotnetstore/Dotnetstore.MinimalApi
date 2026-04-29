using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Configuration;

/// <summary>
/// Covers <c>ApiKeySecurityOptions</c> and verifies its configuration section name plus stable default and initialization behavior.
/// </summary>
public sealed class ApiKeySecurityOptionsTests
{
    [Fact]
    public void SectionName_ShouldMatchExpectedConfigurationSection_WhenReferenced()
    {
        // Assert
        ApiKeySecurityOptions.SectionName.ShouldBe("ApiKey");
    }

    [Fact]
    public void ApiKeySecurityOptions_ShouldUseEmptyStringDefaults_WhenConstructed()
    {
        // Arrange
        var sut = new ApiKeySecurityOptions();

        // Assert
        sut.HeaderName.ShouldBe(string.Empty);
        sut.Value.ShouldBe(string.Empty);
    }

    [Fact]
    public void ApiKeySecurityOptions_ShouldPreserveConfiguredValues_WhenInitialized()
    {
        // Arrange
        const string headerName = "X-CUSTOM-API-KEY";
        const string value = "configured-secret";

        // Act
        var sut = new ApiKeySecurityOptions
        {
            HeaderName = headerName,
            Value = value
        };

        // Assert
        sut.HeaderName.ShouldBe(headerName);
        sut.Value.ShouldBe(value);
    }
}

