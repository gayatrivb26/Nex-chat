using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Jobs;

public class MediaProcessingJob(
    IUnitOfWork uow,
    IFileStorageService storage,
    IVirusScanService virusScan,
    IMediaProcessingService mediaProcessing,
    ILogger<MediaProcessingJob> logger)
{
    public async Task ProcessUploadedMediaAsync(Guid mediaFileId)
    {
        var media = await uow.MediaFiles.GetByIdAsync(mediaFileId);
        if (media == null)
        {
            logger.LogWarning("Media file {MediaFileId} was not found for processing", mediaFileId);
            return;
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "chatapp-media", media.Id.ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var sourcePath = Path.Combine(tempRoot, Path.GetFileName(media.StoredName));
        try
        {
            await storage.DownloadToFileAsync(media.BucketName, media.StoredName, sourcePath);

            var (isClean, scanResult) = await virusScan.ScanByPathAsync(sourcePath);
            media.IsScanned = true;
            media.ScanResult = scanResult;

            if (!isClean)
            {
                logger.LogWarning("Media file {MediaFileId} failed virus scan: {ScanResult}", media.Id, scanResult);
                await storage.DeleteAsync(media.BucketName, media.StoredName);
                uow.MediaFiles.Update(media);
                await uow.SaveChangesAsync();
                return;
            }

            if (IsImage(media.MimeType))
            {
                await using var input = File.OpenRead(sourcePath);
                var (width, height) = await mediaProcessing.GetImageDimensionsAsync(input);
                media.Width = width;
                media.Height = height;
            }

            if (IsVideo(media.MimeType))
                media.Duration = await mediaProcessing.GetVideoDurationAsync(sourcePath);

            if (IsImage(media.MimeType) || IsVideo(media.MimeType))
            {
                var thumbnailPath = await mediaProcessing.GenerateThumbnailAsync(sourcePath, media.BucketName);
                if (!string.IsNullOrWhiteSpace(thumbnailPath) && File.Exists(thumbnailPath))
                {
                    await using var thumbnailStream = File.OpenRead(thumbnailPath);
                    var (_, storedThumbnailPath) = await storage.UploadAsync(
                        thumbnailStream,
                        $"{Path.GetFileNameWithoutExtension(media.OriginalName)}-thumb.jpg",
                        "image/jpeg",
                        media.BucketName);
                    media.ThumbnailPath = storedThumbnailPath;
                    TryDelete(thumbnailPath);
                }
            }

            uow.MediaFiles.Update(media);
            await uow.SaveChangesAsync();
            logger.LogInformation("Processed media file {MediaFileId}", media.Id);
        }
        catch (Exception ex)
        {
            media.IsScanned = true;
            media.ScanResult = "processing_error";
            uow.MediaFiles.Update(media);
            await uow.SaveChangesAsync();
            logger.LogError(ex, "Media processing failed for {MediaFileId}", media.Id);
            throw;
        }
        finally
        {
            TryDelete(sourcePath);
            TryDeleteDirectory(tempRoot);
        }
    }

    private static bool IsImage(string? mimeType)
        => mimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsVideo(string? mimeType)
        => mimeType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true;

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup; the OS temp cleaner can handle leftovers.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup; the OS temp cleaner can handle leftovers.
        }
    }
}
