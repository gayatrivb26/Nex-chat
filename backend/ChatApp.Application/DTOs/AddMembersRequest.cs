using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record AddMembersRequest(Guid ConversationId, List<Guid> UserIds);
