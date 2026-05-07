using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record CreateGroupRequest(
	string Name,
	string? Description,
	List<Guid> MemberIds,
	string? AvatarUrl = null);
