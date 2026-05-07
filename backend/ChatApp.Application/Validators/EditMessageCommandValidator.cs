using ChatApp.Application.Commands.Auth;
using ChatApp.Application.Commands.Conversations;
using ChatApp.Application.Commands.Messages;
using ChatApp.Application.Queries;
using FluentValidation;
namespace ChatApp.Application.Validators;

public class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.NewContent)
            .NotEmpty().WithMessage("Content cannot be empty.")
            .MaximumLength(10000);
    }
}
