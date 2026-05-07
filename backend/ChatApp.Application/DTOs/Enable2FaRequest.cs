using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record Enable2FaRequest(string TotpCode);
