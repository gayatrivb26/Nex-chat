using ChatApp.Application.Commands.Auth;
using ChatApp.Application.Commands.Conversations;
using ChatApp.Application.Commands.Messages;
using ChatApp.Application.Queries;
using FluentValidation;
namespace ChatApp.Application.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        RuleFor(x => x.TotpCode)
            .Length(6, 8).WithMessage("TOTP code must be 6 digits or 8-character backup code.")
            .When(x => !string.IsNullOrEmpty(x.TotpCode));
    }
}
