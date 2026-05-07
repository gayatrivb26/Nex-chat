using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IFileStorageService
{
    Task<(string objectName, string filePath)> UploadAsync(
        Stream fileStream, string fileName, string contentType,
        string bucket, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string bucket, string objectName,
        int expiryMinutes = 60, CancellationToken ct = default);
    Task<string> GetPresignedUploadUrlAsync(string bucket, string objectName,
        int expiryMinutes = 5, CancellationToken ct = default);
    Task DeleteAsync(string bucket, string objectName, CancellationToken ct = default);
    Task<bool> ObjectExistsAsync(string bucket, string objectName, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken ct = default);
}
