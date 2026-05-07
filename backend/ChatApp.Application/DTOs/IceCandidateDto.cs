using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record IceCandidateDto(Guid CallId, Guid TargetUserId, string Candidate, string SdpMid, int SdpMLineIndex);
