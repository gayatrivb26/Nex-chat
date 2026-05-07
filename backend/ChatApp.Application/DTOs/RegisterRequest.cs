using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record RegisterRequest(string Username, string Phone, string Password, string? Email = null);
