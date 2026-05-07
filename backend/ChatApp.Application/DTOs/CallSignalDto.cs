using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record CallSignalDto(Guid CallId, Guid TargetUserId, string Sdp, string SdpType);
