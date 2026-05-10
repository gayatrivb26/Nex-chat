using Hangfire.Dashboard;
namespace ChatApp.API.Filters;

using Hangfire.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

public class HangfireBasicAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public HangfireBasicAuthFilter(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpCtx = context.GetHttpContext();

        if (httpCtx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            return true;

        var authHeader = httpCtx.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Basic ") == true)
        {
            var encoded = authHeader["Basic ".Length..].Trim();
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var parts   = decoded.Split(':', 2);
            if (parts.Length == 2 && parts[0] == _username && parts[1] == _password)
                return true;
        }

        httpCtx.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
        httpCtx.Response.StatusCode = 401;
        return false;
    }
}
