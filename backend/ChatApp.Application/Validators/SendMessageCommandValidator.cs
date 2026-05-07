using ChatApp.Application.Commands.Auth;
using ChatApp.Application.Commands.Conversations;
using ChatApp.Application.Commands.Messages;
using ChatApp.Application.Queries;
using FluentValidation;
namespace ChatApp.Application.Validators;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    private static readonly HashSet<string> ValidTypes =
        new(["text", "image", "video", "audio", "file", "voice", "location", "contact", "sticker"],
            StringComparer.OrdinalIgnoreCase);

    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.SenderId).NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty().When(x => x.MediaUrl == null && x.EncryptedContent == null)
            .WithMessage("Content is required for text messages.")
            .MaximumLength(10000).WithMessage("Message cannot exceed 10,000 characters.")
            .When(x => x.Content != null);

        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(2L * 1024 * 1024 * 1024).WithMessage("File size cannot exceed 2GB.")
            .When(x => x.FileSize.HasValue);
    }
}
