using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IMediaProcessingService
{
    Task<string?> GenerateThumbnailAsync(string sourcePath, string bucket, CancellationToken ct = default);
    Task<(int width, int height)> GetImageDimensionsAsync(Stream imageStream, CancellationToken ct = default);
    Task<Stream> CompressImageAsync(Stream imageStream, int maxWidth = 1920, int quality = 85, CancellationToken ct = default);
    Task<int?> GetVideoDurationAsync(string filePath, CancellationToken ct = default);
}
