using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record Verify2FaRequest(string TotpCode);
