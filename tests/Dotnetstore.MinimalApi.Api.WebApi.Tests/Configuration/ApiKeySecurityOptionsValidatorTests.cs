using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Configuration;

/// <summary>
/// Covers <c>ApiKeySecurityOptionsValidator</c> and verifies fail-fast validation rules for API key configuration.
/// </summary>
public sealed class ApiKeySecurityOptionsValidatorTests
{
    [Fact]
    public void Validate_ShouldSucceed_WhenOptionsAreValid()
    {
        // Arrange
        var sut = new ApiKeySecurityOptionsValidator();
        var options = new ApiKeySecurityOptions
        {
            HeaderName = "X-API-KEY",
            Value = "some-secret-value"
        };

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
        result.Failed.ShouldBeFalse();
    }

    [Theory]
    [InlineData("", "ApiKey:HeaderName cannot be empty.")]
    [InlineData(" ", "ApiKey:HeaderName cannot be empty.")]
    public void Validate_ShouldFail_WhenHeaderNameIsInvalid(
        string invalidHeaderName,
        string expectedError)
    {
        // Arrange
        var sut = new ApiKeySecurityOptionsValidator();
        var options = new ApiKeySecurityOptions
        {
            HeaderName = invalidHeaderName,
            Value = "some-secret-value"
        };

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Theory]
    [InlineData("", "ApiKey:Value cannot be empty.")]
    [InlineData(" ", "ApiKey:Value cannot be empty.")]
    public void Validate_ShouldFail_WhenValueIsInvalid(
        string invalidValue,
        string expectedError)
    {
        // Arrange
        var sut = new ApiKeySecurityOptionsValidator();
        var options = new ApiKeySecurityOptions
        {
            HeaderName = "X-API-KEY",
            Value = invalidValue
        };

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Fact]
    public void Validate_ShouldReturnAllFailures_WhenBothOptionsAreInvalid()
    {
        // Arrange
        var sut = new ApiKeySecurityOptionsValidator();
        var options = new ApiKeySecurityOptions();

        // Act
        var result = sut.Validate(Options.DefaultName, options);
        var failures = result.Failures.ShouldNotBeNull().ToList();

        // Assert
        result.Failed.ShouldBeTrue();
        failures.ShouldContain("ApiKey:HeaderName cannot be empty.");
        failures.ShouldContain("ApiKey:Value cannot be empty.");
    }

    private static void AssertValidationFailure(ValidateOptionsResult result, string expectedError)
    {
        result.Failed.ShouldBeTrue();
        var failures = result.Failures.ShouldNotBeNull().ToList();
        failures.ShouldContain(expectedError);
    }
}
