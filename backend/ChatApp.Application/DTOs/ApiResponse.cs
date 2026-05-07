using ChatApp.Domain.Enums;
namespace ChatApp.Application.DTOs;

public record ApiResponse<T>(bool Success, T? Data, string? Message = null, List<string>? Errors = null)
{
	public static ApiResponse<T> Ok(T data, string? message = null) => new(true, data, message);
	public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new(false, default, message, errors);
}
