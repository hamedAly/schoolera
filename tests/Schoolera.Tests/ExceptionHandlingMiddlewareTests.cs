using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Schoolera.Api.Middleware;
using Schoolera.Api.Resources;
using Schoolera.Application.Common.Models;
using System.Text.Json;

namespace Schoolera.Tests;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenValidationException_ReturnsBadRequestResult()
    {
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException(
            [
                new ValidationFailure("Name", "Name is required.")
            ]),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context, new StubApiMessagesLocalizer());

        var json = await ReadJsonAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(json.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("data").ValueKind);
        Assert.Equal("Name is required.", json.RootElement.GetProperty("errors")[0].GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ReturnsInternalServerErrorResult()
    {
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Database unavailable."),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context, new StubApiMessagesLocalizer());

        var json = await ReadJsonAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.False(json.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("data").ValueKind);
        Assert.Equal("An unexpected error occurred.", json.RootElement.GetProperty("errors")[0].GetString());
        Assert.Equal(ErrorCodes.Unexpected, json.RootElement.GetProperty("errorCodes")[0].GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenClientAborts_DoesNotWrite500OrUnexpectedError()
    {
        var context = CreateHttpContext();
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        context.RequestAborted = aborted.Token;

        var logger = new CollectingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(aborted.Token),
            logger);

        await middleware.InvokeAsync(context, new StubApiMessagesLocalizer());

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
        Assert.DoesNotContain(
            logger.Entries,
            entry => entry.LogLevel == LogLevel.Error);
        Assert.Contains(
            logger.Entries,
            entry =>
                entry.LogLevel == LogLevel.Information &&
                entry.Message.Contains("Client canceled request", StringComparison.Ordinal) &&
                entry.Message.Contains("CorrelationId=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvokeAsync_WhenInternalCancellationWithoutClientAbort_ReturnsUnexpectedError()
    {
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException("internal timeout"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context, new StubApiMessagesLocalizer());

        var json = await ReadJsonAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.Unexpected, json.RootElement.GetProperty("errorCodes")[0].GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenClientAborted_DoesNotWriteFailureResponse()
    {
        var context = CreateHttpContext();
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        context.RequestAborted = aborted.Token;

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException(
            [
                new ValidationFailure("Name", "Name is required.")
            ]),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context, new StubApiMessagesLocalizer());

        Assert.Equal(0, context.Response.Body.Length);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "corr-test-1";

        return context;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;

        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    private sealed class StubApiMessagesLocalizer : IStringLocalizer<ApiMessages>
    {
        public LocalizedString this[string name] =>
            new(name, name == "UnexpectedError" ? "An unexpected error occurred." : name);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            [this["UnexpectedError"]];
    }

    private sealed class CollectingLogger : ILogger<ExceptionHandlingMiddleware>
    {
        public List<(LogLevel LogLevel, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
