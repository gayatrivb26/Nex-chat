namespace ChatApp.API.Middleware;

/// <summary>
/// Adds security headers to every response.
/// CSP is intentionally permissive for development; tighten for production.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private const string CspDev =
        "default-src 'self'; " +
        "connect-src 'self' ws://localhost wss://localhost http://localhost:9000; " +
        "img-src 'self' data: blob: http://localhost:9000; " +
        "media-src 'self' blob:; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "frame-ancestors 'none';";
 
    private const string CspProd =
        "default-src 'self'; " +
        "connect-src 'self' wss: https:; " +
        "img-src 'self' data: blob: https:; " +
        "media-src 'self' blob: https:; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "frame-ancestors 'none';";
 
    public async Task InvokeAsync(HttpContext ctx)
    {
        var h = ctx.Response.Headers;
        h.TryAdd("X-Frame-Options",         "DENY");
        h.TryAdd("X-Content-Type-Options",  "nosniff");
        h.TryAdd("X-XSS-Protection",        "1; mode=block");
        h.TryAdd("Referrer-Policy",         "strict-origin-when-cross-origin");
        h.TryAdd("Permissions-Policy",      "camera=(), microphone=(), geolocation=()");
        h.TryAdd("Content-Security-Policy", env.IsDevelopment() ? CspDev : CspProd);
 
        if (!env.IsDevelopment())
            h.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
 
        await next(ctx);
    }
}