using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Configuration;

/// <summary>
/// Covers <c>WebApiOptions</c> and verifies default option values plus CORS default normalization behavior.
/// </summary>
public sealed class WebApiOptionsTests
{
    [Fact]
    public void WebApiOptions_ShouldInitializeNestedOptions_WhenConstructed()
    {
        // Arrange
        var sut = new WebApiOptions();

        // Assert
        sut.Cors.ShouldNotBeNull();
        sut.Hsts.ShouldNotBeNull();
        sut.HttpsRedirection.ShouldNotBeNull();
        sut.RateLimiting.ShouldNotBeNull();
    }

    [Fact]
    public void ApplyDefaults_ShouldReturnSameOptionsInstance_WhenCalled()
    {
        // Arrange
        var sut = new WebApiOptions();

        // Act
        var result = sut.ApplyDefaults();

        // Assert
        result.ShouldBeSameAs(sut);
    }

    [Fact]
    public void ApplyDefaults_ShouldPopulateCorsValues_WhenCorsArraysAreNull()
    {
        // Arrange
        var sut = WebApiOptionsTestData.CreateValidOptions();
        sut.Cors.AllowedOrigins = null;
        sut.Cors.AllowedMethods = null;

        // Act
        sut.ApplyDefaults();

        // Assert
        sut.Cors.AllowedOrigins.ShouldBeSameAs(WebApiDefaultValues.CorsAllowedOrigins);
        sut.Cors.AllowedMethods.ShouldBeSameAs(WebApiDefaultValues.CorsAllowedMethods);
    }

    [Fact]
    public void ApplyDefaults_ShouldPreserveCorsValues_WhenCorsArraysAreAlreadyConfigured()
    {
        // Arrange
        var configuredOrigins = new[] { "https://frontend.example.com" };
        var configuredMethods = new[] { HttpMethods.Delete };
        var sut = WebApiOptionsTestData.CreateValidOptions();
        sut.Cors.AllowedOrigins = configuredOrigins;
        sut.Cors.AllowedMethods = configuredMethods;

        // Act
        sut.ApplyDefaults();

        // Assert
        sut.Cors.AllowedOrigins.ShouldBeSameAs(configuredOrigins);
        sut.Cors.AllowedMethods.ShouldBeSameAs(configuredMethods);
    }

    [Fact]
    public void WebApiDefaultValues_ShouldExposeExpectedCorsDefaults_WhenAccessed()
    {
        // Assert
        WebApiDefaultValues.CorsAllowedOrigins.ShouldHaveSingleItem().ShouldBe("http://localhost:7000");
        WebApiDefaultValues.CorsAllowedMethods.ShouldBe([HttpMethods.Get, HttpMethods.Post, HttpMethods.Put]);
    }

    [Fact]
    public void WebApiHstsOptions_ShouldUseExpectedDefaults_WhenConstructed()
    {
        // Arrange
        var sut = new WebApiHstsOptions();

        // Assert
        sut.Preload.ShouldBeTrue();
        sut.IncludeSubDomains.ShouldBeTrue();
        sut.MaxAgeDays.ShouldBe(30);
    }

    [Fact]
    public void WebApiHttpsRedirectionOptions_ShouldUseExpectedDefaults_WhenConstructed()
    {
        // Arrange
        var sut = new WebApiHttpsRedirectionOptions();

        // Assert
        sut.RedirectStatusCode.ShouldBe(StatusCodes.Status308PermanentRedirect);
        sut.HttpsPort.ShouldBe(443);
    }

    [Fact]
    public void WebApiRateLimitingOptions_ShouldUseExpectedDefaults_WhenConstructed()
    {
        // Arrange
        var sut = new WebApiRateLimitingOptions();

        // Assert
        sut.RejectionStatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        sut.RejectionMessage.ShouldBe("Too many requests. Please try again later.");
        sut.PartitionKeyFallback.ShouldBe("unknown");
        sut.GlobalPermitLimit.ShouldBe(50);
        sut.GlobalQueueLimit.ShouldBe(10);
        sut.GlobalWindowSeconds.ShouldBe(15);
        sut.ShortPermitLimit.ShouldBe(10);
        sut.ShortQueueLimit.ShouldBe(0);
        sut.ShortWindowSeconds.ShouldBe(15);
    }
}

