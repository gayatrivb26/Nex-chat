using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record AddContactRequest(Guid ContactUserId, string? Nickname = null);
