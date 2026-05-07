using System.IdentityModel.Tokens.Jwt;
using ChatApp.API.Extensions;
using ChatApp.Application.Commands.Auth;
using ChatApp.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatApp.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IMediator mediator, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request, CancellationToken ct)
    {
        var response = await mediator.Send(new RegisterCommand(request.Username, request.Phone, request.Password, request.Email), ct);
        return Ok(ApiResponse<AuthResponse>.Ok(response, "Registration started. Verify OTP to activate the account."));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await mediator.Send(new LoginCommand(
            request.Phone, request.Password, request.TotpCode,
            request.DeviceName, request.DeviceType, HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        SetRefreshCookie(response.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(response with { RefreshToken = string.Empty }));
    }

    [HttpPost("otp/verify")]
    [EnableRateLimiting("otp")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> VerifyOtp(VerifyOtpRequest request, CancellationToken ct)
    {
        var response = await mediator.Send(new VerifyOtpCommand(
            request.Phone, request.Otp, "web", "browser",
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()), ct);

        SetRefreshCookie(response.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(response with { RefreshToken = string.Empty }));
    }

    [HttpPost("otp/send")]
    [EnableRateLimiting("otp-send")]
    public async Task<ActionResult<ApiResponse<object>>> SendOtp(SendOtpRequest request, CancellationToken ct)
    {
        var sent = await mediator.Send(new SendOtpCommand(request.Phone), ct);
        if (!sent)
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<object>.Fail("Too many OTP requests. Please try again later."));

        return Ok(ApiResponse<object>.Ok(new { }, "If the account exists, a verification OTP has been sent."));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(CancellationToken ct)
    {
        var rawToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrWhiteSpace(rawToken))
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Missing refresh token."));

        var response = await mediator.Send(new RefreshTokenCommand(rawToken, HttpContext.Connection.RemoteIpAddress?.ToString()), ct);
        SetRefreshCookie(response.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(response with { RefreshToken = string.Empty }));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refresh_token"];
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;
        var ttl = TimeSpan.FromMinutes(int.TryParse(configuration["Jwt:AccessTokenExpiryMinutes"], out var minutes) ? minutes : 15);

        await mediator.Send(new LogoutCommand(User.GetUserId(), refreshToken, jti, ttl), ct);
        Response.Cookies.Delete("refresh_token");
        return Ok(ApiResponse<object>.Ok(new { }, "Logged out."));
    }

    private void SetRefreshCookie(string refreshToken)
    {
        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/api/v1/auth"
        });
    }
}
