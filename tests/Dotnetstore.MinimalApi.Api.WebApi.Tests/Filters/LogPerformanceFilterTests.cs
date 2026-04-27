using Dotnetstore.MinimalApi.Api.WebApi.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Filters;

/// <summary>
/// Covers <c>LogPerformanceFilter</c> and verifies endpoint filter result passthrough plus performance logging behavior.
/// </summary>
public sealed class LogPerformanceFilterTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReturnNextResult_WhenNextCompletesSuccessfully()
    {
        // Arrange
        var logger = new TestLogger<LogPerformanceFilter>();
        var sut = new LogPerformanceFilter(logger);
        var expectedResult = "ok";
        var invocationCount = 0;
        var context = CreateContext();

        // Act
        var result = await sut.InvokeAsync(context, invocationContext =>
        {
            invocationCount++;
            invocationContext.ShouldBeSameAs(context);

            return ValueTask.FromResult<object?>(expectedResult);
        });

        // Assert
        result.ShouldBe(expectedResult);
        invocationCount.ShouldBe(1);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogExecutionTime_WhenNextCompletesSuccessfully()
    {
        // Arrange
        var logger = new TestLogger<LogPerformanceFilter>();
        var sut = new LogPerformanceFilter(logger);
        var context = CreateContext();

        // Act
        _ = await sut.InvokeAsync(context, _ => ValueTask.FromResult<object?>(Results.Ok()));

        var entry = logger.Entries.ShouldHaveSingleItem();

        // Assert
        entry.LogLevel.ShouldBe(LogLevel.Information);
        entry.Message.ShouldContain("Endpoint execution time:");
        entry.Message.ShouldContain(" ms");
        entry.EventId.ShouldBe(default);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogExecutionTime_WhenNextThrows()
    {
        // Arrange
        var logger = new TestLogger<LogPerformanceFilter>();
        var sut = new LogPerformanceFilter(logger);
        var context = CreateContext();
        var expectedException = new InvalidOperationException("Boom");

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.InvokeAsync(context, _ => ValueTask.FromException<object?>(expectedException)).AsTask());

        var entry = logger.Entries.ShouldHaveSingleItem();

        // Assert
        exception.ShouldBeSameAs(expectedException);
        entry.LogLevel.ShouldBe(LogLevel.Information);
        entry.Message.ShouldContain("Endpoint execution time:");
        entry.Message.ShouldContain(" ms");
        entry.EventId.ShouldBe(default);
    }

    private static DefaultEndpointFilterInvocationContext CreateContext() =>
        new(new DefaultHttpContext(), []);

    private sealed record LogEntry(LogLevel LogLevel, EventId EventId, string Message);

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
            _entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception)));
        }
    }
}

