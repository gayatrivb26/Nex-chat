using ChatApp.Application.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace ChatApp.API.Middleware;

/// <summary>
/// Centralized exception → ProblemDetails mapper.
/// Replaces app.UseExceptionHandler() with no-arg form.
/// Register with: app.UseExceptionHandler("/error") or via IExceptionHandler.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message, errors) = MapException(exception);

        logger.LogError(exception,
            "Unhandled exception [{Type}] on {Method} {Path}: {Message}",
            exception.GetType().Name,
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception.Message);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(message, errors);
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions), cancellationToken);

        return true;
    }

    private static (int statusCode, string message, List<string>? errors) MapException(Exception ex)
        => ex switch
        {
            ValidationException ve => (
                (int)HttpStatusCode.BadRequest,
                "Validation failed.",
                ve.Errors.Select(e => e.ErrorMessage).ToList()),

            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                ex.Message,
                null),

            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                ex.Message,
                null),

            InvalidOperationException when ex.Message == "2FA_REQUIRED" => (
                (int)HttpStatusCode.PreconditionRequired,
                "Two-factor authentication code required.",
                null),

            InvalidOperationException => (
                (int)HttpStatusCode.BadRequest,
                ex.Message,
                null),

            OperationCanceledException => (
                499, // Client Closed Request (nginx convention)
                "Request was cancelled.",
                null),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again.",
                null)
        };
}