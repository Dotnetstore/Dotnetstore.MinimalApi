using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Dotnetstore.MinimalApi.Api.WebApi.Filters;
using Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Filters;

/// <summary>
/// Covers <c>ApiKeyFilter</c> and verifies fail-fast unauthorized responses plus valid API-key passthrough behavior.
/// </summary>
public sealed class ApiKeyFilterTests
{
    [Theory]
    [InlineData("", TestHttp.ApiKeyValue)]
    [InlineData(" ", TestHttp.ApiKeyValue)]
    [InlineData(TestHttp.ApiKeyHeaderName, "")]
    [InlineData(TestHttp.ApiKeyHeaderName, " ")]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenApiKeyOptionsAreInvalid(
        string headerName,
        string apiKeyValue)
    {
        // Arrange
        var sut = CreateSut(headerName, apiKeyValue);
        var context = CreateContext();
        var invocationCount = 0;

        // Act
        var result = await sut.InvokeAsync(context, _ =>
        {
            invocationCount++;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        // Assert
        invocationCount.ShouldBe(0);
        await AssertUnauthorizedResultAsync(result, context.HttpContext);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext();
        var invocationCount = 0;

        // Act
        var result = await sut.InvokeAsync(context, _ =>
        {
            invocationCount++;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        // Assert
        invocationCount.ShouldBe(0);
        await AssertUnauthorizedResultAsync(result, context.HttpContext);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenApiKeyHeaderValueIsIncorrect()
    {
        // Arrange
        const string invalidApiKey = "invalid-api-key";
        var sut = CreateSut();
        var context = CreateContext((TestHttp.ApiKeyHeaderName, invalidApiKey));
        var invocationCount = 0;

        // Act
        var result = await sut.InvokeAsync(context, _ =>
        {
            invocationCount++;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        // Assert
        invocationCount.ShouldBe(0);
        await AssertUnauthorizedResultAsync(result, context.HttpContext);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnNextResult_WhenApiKeyHeaderValueMatchesConfiguredValue()
    {
        // Arrange
        var expectedResult = "ok";
        var sut = CreateSut();
        var context = CreateContext((TestHttp.ApiKeyHeaderName, TestHttp.ApiKeyValue));
        var invocationCount = 0;

        // Act
        var result = await sut.InvokeAsync(context, invocationContext =>
        {
            invocationCount++;
            invocationContext.ShouldBeSameAs(context);

            return ValueTask.FromResult<object?>(expectedResult);
        });

        // Assert
        invocationCount.ShouldBe(1);
        result.ShouldBe(expectedResult);
    }

    private static ApiKeyFilter CreateSut(
        string headerName = TestHttp.ApiKeyHeaderName,
        string apiKeyValue = TestHttp.ApiKeyValue) =>
        new(Options.Create(new ApiKeySecurityOptions
        {
            HeaderName = headerName,
            Value = apiKeyValue
        }));

    private static DefaultEndpointFilterInvocationContext CreateContext(params (string Name, string Value)[] headers)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        foreach (var (name, value) in headers)
        {
            httpContext.Request.Headers.Append(name, value);
        }

        return new DefaultEndpointFilterInvocationContext(httpContext, []);
    }

    private static async Task AssertUnauthorizedResultAsync(object? result, HttpContext httpContext)
    {
        var unauthorizedResult = result.ShouldBeAssignableTo<IResult>();
        unauthorizedResult.ShouldNotBeNull();

        await unauthorizedResult.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }
}

