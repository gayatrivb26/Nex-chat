using ChatApp.Domain.Interfaces;

namespace ChatApp.Infrastructure.Services;

public class VirusScanService : IVirusScanService
{
    public Task<(bool isClean, string result)> ScanAsync(Stream fileStream, CancellationToken ct = default)
        => Task.FromResult((true, "queued"));

    public Task<(bool isClean, string result)> ScanByPathAsync(string filePath, CancellationToken ct = default)
        => Task.FromResult((true, "queued"));
}
