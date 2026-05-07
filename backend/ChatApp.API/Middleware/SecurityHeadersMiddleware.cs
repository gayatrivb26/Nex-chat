namespace ChatApp.API.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.TryAdd("X-Frame-Options", "DENY");
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        headers.TryAdd("Content-Security-Policy",
            "default-src 'self'; connect-src 'self' wss: https:; img-src 'self' data: blob: https:; media-src 'self' blob: https:; script-src 'self'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'");

        if (context.Request.IsHttps)
            headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        await next(context);
    }
}
