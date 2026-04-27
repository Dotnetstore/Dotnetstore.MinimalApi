using System.ComponentModel.DataAnnotations;
using Dotnetstore.MinimalApi.Api.WebApi.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Exceptions;

/// <summary>
/// Covers <c>DefaultExceptionHandler</c> and verifies exception-to-status mapping, problem details generation, and error logging.
/// </summary>
public sealed class DefaultExceptionHandlerTests
{
    [Theory]
    [MemberData(nameof(ExceptionMappings))]
    public async Task TryHandleAsync_ShouldMapExceptionToExpectedStatusCode_WhenExceptionIsHandled(
        Exception exception,
        int expectedStatusCode)
    {
        // Arrange
        var problemDetailsService = new CapturingProblemDetailsService();
        var logger = new TestLogger<DefaultExceptionHandler>();
        var sut = new DefaultExceptionHandler(problemDetailsService, logger);
        var httpContext = CreateHttpContext();

        // Act
        var handled = await sut.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        // Assert
        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(expectedStatusCode);
        problemDetailsService.CapturedContext.ShouldNotBeNull();
        problemDetailsService.CapturedContext.ProblemDetails.ShouldNotBeNull();
        problemDetailsService.CapturedContext.ProblemDetails.Status.ShouldBe(expectedStatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldWriteProblemDetailsAndLogError_WhenExceptionOccurs()
    {
        // Arrange
        var problemDetailsService = new CapturingProblemDetailsService();
        var logger = new TestLogger<DefaultExceptionHandler>();
        var sut = new DefaultExceptionHandler(problemDetailsService, logger);
        var exception = new ValidationException("Bad input.");
        var httpContext = CreateHttpContext(HttpMethods.Post, "/test");

        // Act
        var handled = await sut.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);
        var context = problemDetailsService.CapturedContext.ShouldNotBeNull();
        var problemDetails = context.ProblemDetails.ShouldNotBeNull();
        var entry = logger.Entries.ShouldHaveSingleItem();

        // Assert
        handled.ShouldBeTrue();
        context.HttpContext.ShouldBeSameAs(httpContext);
        context.Exception.ShouldBeSameAs(exception);
        problemDetails.Type.ShouldBe("bad-request");
        problemDetails.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problemDetails.Title.ShouldBe("The request is invalid.");
        problemDetails.Detail.ShouldBe(exception.Message);
        problemDetails.Instance.ShouldBe("POST /test");
        entry.LogLevel.ShouldBe(LogLevel.Error);
        entry.EventId.ShouldBe(default);
        entry.Exception.ShouldBeSameAs(exception);
        entry.Message.ShouldBe("An unhandled exception occurred while processing the request.");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldHideInternalDetails_WhenExceptionMapsToInternalServerError()
    {
        // Arrange
        var problemDetailsService = new CapturingProblemDetailsService();
        var logger = new TestLogger<DefaultExceptionHandler>();
        var sut = new DefaultExceptionHandler(problemDetailsService, logger);
        var exception = new InvalidOperationException("Database connection details should stay internal.");
        var httpContext = CreateHttpContext(HttpMethods.Get, "/fail");

        // Act
        var handled = await sut.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);
        var problemDetails = problemDetailsService.CapturedContext.ShouldNotBeNull().ProblemDetails.ShouldNotBeNull();

        // Assert
        handled.ShouldBeTrue();
        problemDetails.Type.ShouldBe("internal-server-error");
        problemDetails.Status.ShouldBe(StatusCodes.Status500InternalServerError);
        problemDetails.Title.ShouldBe("An unexpected error occurred while processing your request.");
        var detail = problemDetails.Detail.ShouldNotBeNull();
        detail.ShouldBe("An unexpected error occurred.");
        detail.ShouldNotContain("Database connection details");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnProblemDetailsServiceResult_WhenWritingProblemDetailsFails()
    {
        // Arrange
        var problemDetailsService = new CapturingProblemDetailsService(shouldWrite: false);
        var logger = new TestLogger<DefaultExceptionHandler>();
        var sut = new DefaultExceptionHandler(problemDetailsService, logger);
        var exception = new InvalidOperationException("Unexpected failure.");
        var httpContext = CreateHttpContext(HttpMethods.Get, "/fail");

        // Act
        var handled = await sut.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        // Assert
        handled.ShouldBeFalse();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemDetailsService.CapturedContext.ShouldNotBeNull();
        logger.Entries.ShouldHaveSingleItem();
    }

    public static TheoryData<Exception, int> ExceptionMappings() => new()
    {
        { new ValidationException("Validation failed."), StatusCodes.Status400BadRequest },
        { new KeyNotFoundException("Missing resource."), StatusCodes.Status404NotFound },
        { new UnauthorizedAccessException("Unauthorized."), StatusCodes.Status401Unauthorized },
        { new InvalidOperationException("Unexpected failure."), StatusCodes.Status500InternalServerError }
    };

    private static DefaultHttpContext CreateHttpContext(
        string method = "GET",
        string path = "/errors")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;

        return httpContext;
    }

    private sealed class CapturingProblemDetailsService(bool shouldWrite = true) : IProblemDetailsService
    {
        internal ProblemDetailsContext? CapturedContext { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            CapturedContext = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            CapturedContext = context;
            return ValueTask.FromResult(shouldWrite);
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, EventId EventId, string Message, Exception? Exception);

    private sealed class TestLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> _entries = [];

        internal IReadOnlyList<LogEntry> Entries => _entries;

        IDisposable? ILogger.BeginScope<TState>(TState state) where TState : default => null;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
        }
    }
}

