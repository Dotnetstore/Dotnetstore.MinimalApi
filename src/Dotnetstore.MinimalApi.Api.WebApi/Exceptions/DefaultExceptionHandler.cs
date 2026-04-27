using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Dotnetstore.MinimalApi.Api.WebApi.Exceptions;

internal sealed class DefaultExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DefaultExceptionHandler> logger) : IExceptionHandler
{
    private const string InternalServerErrorDetail = "An unexpected error occurred.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred while processing the request.");

        var problemDetails = CreateProblemDetails(httpContext, exception);
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        var statusCode = GetStatusCode(exception);

        return new ProblemDetails
        {
            Type = GetProblemType(statusCode),
            Status = statusCode,
            Title = GetProblemTitle(statusCode),
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? InternalServerErrorDetail
                : exception.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        ValidationException => StatusCodes.Status400BadRequest,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string GetProblemTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "The request is invalid.",
        StatusCodes.Status401Unauthorized => "Authentication is required.",
        StatusCodes.Status404NotFound => "The requested resource was not found.",
        _ => "An unexpected error occurred while processing your request."
    };

    private static string GetProblemType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "bad-request",
        StatusCodes.Status401Unauthorized => "unauthorized",
        StatusCodes.Status404NotFound => "not-found",
        _ => "internal-server-error"
    };
}