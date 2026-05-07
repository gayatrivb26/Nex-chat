using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IVirusScanService
{
    Task<(bool isClean, string result)> ScanAsync(Stream fileStream, CancellationToken ct = default);
    Task<(bool isClean, string result)> ScanByPathAsync(string filePath, CancellationToken ct = default);
}
