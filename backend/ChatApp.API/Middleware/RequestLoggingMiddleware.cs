namespace ChatApp.API.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                         ?? Guid.NewGuid().ToString("N")[..16];
 
        ctx.Response.Headers.TryAdd("X-Correlation-Id", correlationId);
 
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            await next(ctx);
    }
}