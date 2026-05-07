using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Application.Commands.Auth;

public class SendOtpHandler(
    IOtpService otpService,
    IUserRepository userRepository,
    IEmailService emailService,
    ILogger<SendOtpHandler> logger) : IRequestHandler<SendOtpCommand, bool>
{
    public async Task<bool> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByPhoneAsync(request.Phone, cancellationToken);
        if (user == null)
            return true; // avoid user enumeration

        var sendAllowed = await otpService.TryMarkOtpSendAsync(request.Phone, 3, TimeSpan.FromMinutes(10), cancellationToken);
        if (!sendAllowed)
            return false;

        var otp = await otpService.GenerateAndStoreOtpAsync(request.Phone, cancellationToken);
        if (!string.IsNullOrWhiteSpace(user.Email))
            await emailService.SendOtpEmailAsync(user.Email, otp, cancellationToken);

        logger.LogInformation("OTP resent for user {UserId}", user.Id);
        return true;
    }
}
