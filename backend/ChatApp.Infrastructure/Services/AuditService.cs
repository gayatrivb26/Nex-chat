using System.Text.Json;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Data;

namespace ChatApp.Infrastructure.Services;

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task LogAsync(string action, Guid? userId = null, string? entityType = null,
        Guid? entityId = null, object? oldValues = null, object? newValues = null,
        string? ip = null, string? userAgent = null, CancellationToken ct = default)
    {
        var oldJson = oldValues == null ? null : JsonSerializer.Serialize(oldValues);
        var newJson = newValues == null ? null : JsonSerializer.Serialize(newValues);
        await db.AuditLogs.AddAsync(AuditLog.Create(action, userId, entityType, entityId, oldJson, newJson, ip, userAgent), ct);
        await db.SaveChangesAsync(ct);
    }
}
