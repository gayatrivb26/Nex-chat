using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, Guid? userId = null, string? entityType = null,
        Guid? entityId = null, object? oldValues = null, object? newValues = null,
        string? ip = null, string? userAgent = null, CancellationToken ct = default);
}
