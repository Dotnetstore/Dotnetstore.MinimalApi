using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Configuration;

/// <summary>
/// Covers <c>WebApiOptionsValidator</c> and verifies success plus fail-fast validation rules for malformed Web API configuration.
/// </summary>
public sealed class WebApiOptionsValidatorTests
{
    [Fact]
    public void Validate_ShouldSucceed_WhenOptionsAreValid()
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
        result.Failed.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(InvalidCorsOrigins))]
    public void Validate_ShouldFail_WhenCorsAllowedOriginsAreInvalid(
        string[]? invalidOrigins,
        string expectedError)
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.Cors.AllowedOrigins = invalidOrigins;

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Theory]
    [MemberData(nameof(InvalidCorsMethods))]
    public void Validate_ShouldFail_WhenCorsAllowedMethodsAreInvalid(
        string[]? invalidMethods,
        string expectedError)
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.Cors.AllowedMethods = invalidMethods;

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Theory]
    [InlineData(0, "WebApi:Hsts:MaxAgeDays must be greater than 0.")]
    public void Validate_ShouldFail_WhenHstsOptionsAreInvalid(
        int invalidMaxAgeDays,
        string expectedError)
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.Hsts.MaxAgeDays = invalidMaxAgeDays;

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Fact]
    public void Validate_ShouldAllowInvalidHstsMaxAge_WhenHstsIsDisabled()
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.Hsts.Enabled = false;
        options.Hsts.MaxAgeDays = 0;

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidOpenTelemetryTextOptions))]
    public void Validate_ShouldFail_WhenOpenTelemetryTextOptionIsInvalid(
        string invalidValue,
        string expectedError,
        string propertyName)
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();

        switch (propertyName)
        {
            case nameof(WebApiOpenTelemetryOptions.ServiceName):
                options.OpenTelemetry.ServiceName = invalidValue;
                break;
            case nameof(WebApiOpenTelemetryOptions.ServiceVersion):
                options.OpenTelemetry.ServiceVersion = invalidValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, "Unknown invalid OpenTelemetry text property.");
        }

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Fact]
    public void Validate_ShouldFail_WhenOpenTelemetryTracingExcludedPathsContainEmptyValue()
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.OpenTelemetry.Tracing.ExcludedPaths = ["/openapi", " "];

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, "WebApi:OpenTelemetry:Tracing:ExcludedPaths cannot contain empty values.");
    }

    [Fact]
    public void Validate_ShouldAllowTelemetrySignalsToBeDisabled_WhenTracingAndMetricsAreDisabled()
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.OpenTelemetry.Tracing.Enabled = false;
        options.OpenTelemetry.Metrics.Enabled = false;

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(299, "WebApi:HttpsRedirection:RedirectStatusCode must be a valid redirect status code between 300 and 399.")]
    [InlineData(400, "WebApi:HttpsRedirection:RedirectStatusCode must be a valid redirect status code between 300 and 399.")]
    public void Validate_ShouldFail_WhenHttpsRedirectStatusCodeIsInvalid(
        int invalidRedirectStatusCode,
        string expectedError)
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.HttpsRedirection.RedirectStatusCode = invalidRedirectStatusCode;

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Theory]
    [InlineData(0, "WebApi:HttpsRedirection:HttpsPort must be between 1 and 65535.")]
    [InlineData(65536, "WebApi:HttpsRedirection:HttpsPort must be between 1 and 65535.")]
    public void Validate_ShouldFail_WhenHttpsPortIsInvalid(
        int invalidHttpsPort,
        string expectedError)
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.HttpsRedirection.HttpsPort = invalidHttpsPort;

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Fact]
    public void Validate_ShouldAllowInvalidHttpsRedirectionValues_WhenHttpsRedirectionIsDisabled()
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.HttpsRedirection.Enabled = false;
        options.HttpsRedirection.RedirectStatusCode = 200;
        options.HttpsRedirection.HttpsPort = 0;

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(399, "WebApi:RateLimiting:RejectionStatusCode must be between 400 and 599.")]
    [InlineData(600, "WebApi:RateLimiting:RejectionStatusCode must be between 400 and 599.")]
    public void Validate_ShouldFail_WhenRateLimitingStatusCodeIsInvalid(
        int invalidRejectionStatusCode,
        string expectedError)
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.RateLimiting.RejectionStatusCode = invalidRejectionStatusCode;

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Theory]
    [MemberData(nameof(InvalidRateLimitingTextOptions))]
    public void Validate_ShouldFail_WhenRateLimitingTextOptionIsInvalid(
        string invalidValue,
        string expectedError,
        bool isPartitionKeyFallback)
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();

        if (isPartitionKeyFallback)
        {
            options.RateLimiting.PartitionKeyFallback = invalidValue;
        }
        else
        {
            options.RateLimiting.RejectionMessage = invalidValue;
        }

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    [Fact]
    public void Validate_ShouldReturnAllFailures_WhenMultipleOptionsAreInvalid()
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();
        options.Cors.AllowedOrigins = [];
        options.Hsts.MaxAgeDays = 0;
        options.OpenTelemetry.ServiceName = string.Empty;
        options.RateLimiting.RejectionMessage = string.Empty;

        // Act
        var result = sut.Validate(Options.DefaultName, options);
        var failures = result.Failures.ShouldNotBeNull().ToList();

        // Assert
        result.Failed.ShouldBeTrue();
        failures.ShouldContain("WebApi:Cors:AllowedOrigins must contain at least one value.");
        failures.ShouldContain("WebApi:Hsts:MaxAgeDays must be greater than 0.");
        failures.ShouldContain("WebApi:OpenTelemetry:ServiceName cannot be empty.");
        failures.ShouldContain("WebApi:RateLimiting:RejectionMessage cannot be empty.");
    }

    public static TheoryData<string[]?, string> InvalidCorsOrigins() => new()
    {
        { null, "WebApi:Cors:AllowedOrigins must contain at least one value." },
        { [], "WebApi:Cors:AllowedOrigins must contain at least one value." },
        { [" ", "http://localhost:7000"], "WebApi:Cors:AllowedOrigins cannot contain empty values." }
    };

    public static TheoryData<string[]?, string> InvalidCorsMethods() => new()
    {
        { null, "WebApi:Cors:AllowedMethods must contain at least one value." },
        { [], "WebApi:Cors:AllowedMethods must contain at least one value." },
        { [HttpMethods.Get, " "], "WebApi:Cors:AllowedMethods cannot contain empty values." }
    };

    public static TheoryData<string, string, string> InvalidOpenTelemetryTextOptions() => new()
    {
        { " ", "WebApi:OpenTelemetry:ServiceName cannot be empty.", nameof(WebApiOpenTelemetryOptions.ServiceName) },
        { " ", "WebApi:OpenTelemetry:ServiceVersion cannot be empty when provided.", nameof(WebApiOpenTelemetryOptions.ServiceVersion) }
    };

    public static TheoryData<string, string, bool> InvalidRateLimitingTextOptions() => new()
    {
        { " ", "WebApi:RateLimiting:RejectionMessage cannot be empty.", false },
        { "", "WebApi:RateLimiting:PartitionKeyFallback cannot be empty.", true }
    };

    [Theory]
    [InlineData("GlobalPermitLimit", 0, "WebApi:RateLimiting:GlobalPermitLimit must be greater than 0.")]
    [InlineData("GlobalQueueLimit", -1, "WebApi:RateLimiting:GlobalQueueLimit cannot be negative.")]
    [InlineData("GlobalWindowSeconds", 0, "WebApi:RateLimiting:GlobalWindowSeconds must be greater than 0.")]
    [InlineData("ShortPermitLimit", 0, "WebApi:RateLimiting:ShortPermitLimit must be greater than 0.")]
    [InlineData("ShortQueueLimit", -1, "WebApi:RateLimiting:ShortQueueLimit cannot be negative.")]
    [InlineData("ShortWindowSeconds", 0, "WebApi:RateLimiting:ShortWindowSeconds must be greater than 0.")]
    public void Validate_ShouldFail_WhenRateLimitingNumericOptionIsInvalid(
        string propertyName,
        int invalidValue,
        string expectedError)
    {
        // Arrange
        var sut = new WebApiOptionsValidator();
        var options = WebApiOptionsTestData.CreateValidOptions();

        switch (propertyName)
        {
            case "GlobalPermitLimit":
                options.RateLimiting.GlobalPermitLimit = invalidValue;
                break;
            case "GlobalQueueLimit":
                options.RateLimiting.GlobalQueueLimit = invalidValue;
                break;
            case "GlobalWindowSeconds":
                options.RateLimiting.GlobalWindowSeconds = invalidValue;
                break;
            case "ShortPermitLimit":
                options.RateLimiting.ShortPermitLimit = invalidValue;
                break;
            case "ShortQueueLimit":
                options.RateLimiting.ShortQueueLimit = invalidValue;
                break;
            case "ShortWindowSeconds":
                options.RateLimiting.ShortWindowSeconds = invalidValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, "Unknown invalid rate limiting numeric property.");
        }

        // Act
        var result = sut.Validate(Options.DefaultName, options);

        // Assert
        AssertValidationFailure(result, expectedError);
    }

    private static void AssertValidationFailure(ValidateOptionsResult result, string expectedError)
    {
        result.Failed.ShouldBeTrue();
        var failures = result.Failures.ShouldNotBeNull().ToList();
        failures.ShouldContain(expectedError);
    }
}

