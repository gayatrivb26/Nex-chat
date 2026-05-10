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
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web);
 
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception exception, CancellationToken ct)
    {
        var (status, message, errors) = MapException(exception);
 
        if (status == 500)
            logger.LogError(exception, "Unhandled exception [{Type}] {Method} {Path}",
                exception.GetType().Name, ctx.Request.Method, ctx.Request.Path);
 
        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";
 
        var body = new
        {
            statusCode = status,
            message,
            errors,
            traceId = ctx.TraceIdentifier
        };
 
        await ctx.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(body, JsonOptions), ct);
 
        return true;
    }
 
    private static (int status, string message, List<string>? errors) MapException(Exception ex)
        => ex switch
        {
            FluentValidation.ValidationException ve => (400, "Validation failed.",
                ve.Errors.Select(e => e.ErrorMessage).ToList()),
            KeyNotFoundException         => (404, ex.Message, null),
            UnauthorizedAccessException  => (401, ex.Message, null),
            InvalidOperationException { Message: "2FA_REQUIRED" }
                                         => (428, "Two-factor authentication code required.", null),
            InvalidOperationException    => (400, ex.Message, null),
            OperationCanceledException   => (499, "Request cancelled.", null),
            _                            => (500, "An unexpected error occurred.", null)
        };
}