using ChatApp.Domain.Interfaces;

namespace ChatApp.Infrastructure.Services;

public class MediaProcessingService : IMediaProcessingService
{
    public Task<string?> GenerateThumbnailAsync(string sourcePath, string bucket, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public Task<(int width, int height)> GetImageDimensionsAsync(Stream imageStream, CancellationToken ct = default)
        => Task.FromResult((0, 0));

    public Task<Stream> CompressImageAsync(Stream imageStream, int maxWidth = 1920, int quality = 85, CancellationToken ct = default)
        => Task.FromResult(imageStream);

    public Task<int?> GetVideoDurationAsync(string filePath, CancellationToken ct = default)
        => Task.FromResult<int?>(null);
}
