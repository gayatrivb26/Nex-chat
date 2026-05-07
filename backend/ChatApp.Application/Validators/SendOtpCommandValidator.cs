using ChatApp.Application.Commands.Auth;
using FluentValidation;

namespace ChatApp.Application.Validators;

public class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+[1-9]\d{6,14}$").WithMessage("Phone must be in E.164 format (e.g. +919876543210).");
    }
}
