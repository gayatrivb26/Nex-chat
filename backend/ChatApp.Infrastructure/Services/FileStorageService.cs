using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ChatApp.Infrastructure.Services;

public class FileStorageService(IConfiguration configuration) : IFileStorageService
{
    public Task<(string objectName, string filePath)> UploadAsync(
        Stream fileStream, string fileName, string contentType,
        string bucket, CancellationToken ct = default)
    {
        var objectName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        return Task.FromResult((objectName, $"{bucket}/{objectName}"));
    }

    public Task<string> GetPresignedUrlAsync(string bucket, string objectName,
        int expiryMinutes = 60, CancellationToken ct = default)
    {
        var publicBaseUrl = configuration["MinIO:PublicUrl"] ?? "http://localhost:9000";
        return Task.FromResult($"{publicBaseUrl.TrimEnd('/')}/{bucket}/{Uri.EscapeDataString(objectName)}?expires={expiryMinutes}");
    }

    public Task<string> GetPresignedUploadUrlAsync(string bucket, string objectName,
        int expiryMinutes = 5, CancellationToken ct = default)
    {
        var publicBaseUrl = configuration["MinIO:PublicUrl"] ?? "http://localhost:9000";
        return Task.FromResult($"{publicBaseUrl.TrimEnd('/')}/{bucket}/{Uri.EscapeDataString(objectName)}?upload=1&expires={expiryMinutes}");
    }

    public Task DeleteAsync(string bucket, string objectName, CancellationToken ct = default) => Task.CompletedTask;

    public Task<bool> ObjectExistsAsync(string bucket, string objectName, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken ct = default)
        => Task.FromResult<Stream>(Stream.Null);
}
