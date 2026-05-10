using ChatApp.API.Extensions;
using ChatApp.API.Metrics;
using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/media")]
public class MediaController(
    IUnitOfWork uow,
    IFileStorageService storage,
    IBackgroundJobClient backgroundJobs) : ControllerBase
{
    [HttpPost("upload/init")]
    [EnableRateLimiting("uploads")]
    public async Task<ActionResult<ApiResponse<UploadInitResponse>>> InitUpload(UploadInitRequest request, CancellationToken ct)
    {
        ValidateFile(request);

        var bucket = request.FileType.ToLowerInvariant() switch
        {
            "avatar" => "chat-avatars",
            "image" or "video" or "voice" => "chat-media",
            _ => "chat-files"
        };

        var safeFileName = SanitizeFileName(request.FileName);
        var objectName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFileName}";
        var uploadUrl = await storage.GetPresignedUploadUrlAsync(bucket, objectName, 5, ct);
        var media = MediaFile.Create(User.GetUserId(), safeFileName, objectName, bucket, $"{bucket}/{objectName}", request.ContentType, request.FileSize);
        await uow.MediaFiles.AddAsync(media, ct);
        await uow.SaveChangesAsync(ct);

        return Ok(ApiResponse<UploadInitResponse>.Ok(new UploadInitResponse(media.Id, uploadUrl, objectName, DateTime.UtcNow.AddMinutes(5))));
    }

    [HttpPost("upload/complete")]
    public async Task<ActionResult<ApiResponse<MediaFileDto>>> CompleteUpload(UploadCompleteRequest request, CancellationToken ct)
    {
        var media = await uow.MediaFiles.GetByIdAsync(request.FileId, ct)
            ?? throw new KeyNotFoundException("Media file not found.");

        if (media.UploadedById != User.GetUserId())
            return Forbid();

        if (!string.Equals(media.StoredName, request.ObjectName, StringComparison.Ordinal))
            throw new InvalidOperationException("Upload object name does not match the initialized upload.");

        if (!await storage.ObjectExistsAsync(media.BucketName, media.StoredName, ct))
            throw new InvalidOperationException("Uploaded object was not found in storage.");

        ChatMetrics.FileUploadBytesTotal.Inc(media.FileSize ?? 0);
        backgroundJobs.Enqueue<MediaProcessingJob>(job => job.ProcessUploadedMediaAsync(media.Id));
        var url = await storage.GetPresignedUrlAsync(media.BucketName, media.StoredName, 60, ct);

        return Ok(ApiResponse<MediaFileDto>.Ok(new MediaFileDto(
            media.Id, media.OriginalName, media.MimeType, media.FileSize, url,
            media.ThumbnailPath, media.Width, media.Height, media.Duration, media.CreatedAt)));
    }

    private static void ValidateFile(UploadInitRequest request)
    {
        var maxBytes = request.FileType.ToLowerInvariant() switch
        {
            "image" => 25L * 1024 * 1024,
            "video" => 500L * 1024 * 1024,
            "voice" => 16L * 1024 * 1024,
            _ => 2L * 1024 * 1024 * 1024
        };

        if (request.FileSize <= 0 || request.FileSize > maxBytes)
            throw new InvalidOperationException("File size exceeds the allowed limit.");

        if (string.IsNullOrWhiteSpace(request.FileName) || request.FileName.Length > 255)
            throw new InvalidOperationException("Invalid file name.");

        var contentType = request.ContentType.Trim().ToLowerInvariant();
        if (!IsAllowedMimeType(request.FileType, contentType))
            throw new InvalidOperationException("Invalid content type.");
    }

    private static bool IsAllowedMimeType(string fileType, string contentType)
        => fileType.ToLowerInvariant() switch
        {
            "avatar" => contentType is "image/jpeg" or "image/png" or "image/webp",
            "image" => contentType is "image/jpeg" or "image/png" or "image/gif" or "image/webp" or "image/heic",
            "video" => contentType is "video/mp4" or "video/webm" or "video/quicktime",
            "voice" => contentType is "audio/mpeg" or "audio/mp4" or "audio/ogg" or "audio/wav" or "audio/webm",
            _ => contentType.Contains('/') && !IsDangerousMimeType(contentType)
        };

    private static bool IsDangerousMimeType(string contentType)
        => contentType is "application/x-msdownload"
            or "application/x-msdos-program"
            or "application/x-sh"
            or "application/x-bat"
            or "application/x-csh"
            or "application/x-msi"
            or "text/html"
            or "application/javascript"
            or "text/javascript";

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Invalid file name.");

        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return name;
    }
}
