using ChatApp.Application.Commands.Auth;
using ChatApp.Application.Commands.Conversations;
using ChatApp.Application.Commands.Messages;
using ChatApp.Application.Queries;
using FluentValidation;
namespace ChatApp.Application.Validators;

public class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Search query is required.")
            .MinimumLength(2).WithMessage("Search query must be at least 2 characters.")
            .MaximumLength(50);
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
    }
}
