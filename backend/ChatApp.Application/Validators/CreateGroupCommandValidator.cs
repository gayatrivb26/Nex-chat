using ChatApp.Application.Commands.Auth;
using ChatApp.Application.Commands.Conversations;
using ChatApp.Application.Commands.Messages;
using ChatApp.Application.Queries;
using FluentValidation;
namespace ChatApp.Application.Validators;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(100).WithMessage("Group name cannot exceed 100 characters.")
            .MinimumLength(2);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);

        RuleFor(x => x.MemberIds)
            .Must(ids => ids.Count >= 1).WithMessage("Group must have at least 1 other member.")
            .Must(ids => ids.Count <= 255).WithMessage("Group cannot have more than 256 members.");
    }
}
