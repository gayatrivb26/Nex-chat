using ChatApp.API.Extensions;
using ChatApp.API.Metrics;
using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/media")]
public class MediaController(IUnitOfWork uow, IFileStorageService storage) : ControllerBase
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

        var objectName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{Path.GetFileName(request.FileName)}";
        var uploadUrl = await storage.GetPresignedUploadUrlAsync(bucket, objectName, 5, ct);
        var media = MediaFile.Create(User.GetUserId(), request.FileName, objectName, bucket, $"{bucket}/{objectName}", request.ContentType, request.FileSize);
        await uow.MediaFiles.AddAsync(media, ct);
        await uow.SaveChangesAsync(ct);

        return Ok(ApiResponse<UploadInitResponse>.Ok(new UploadInitResponse(media.Id, uploadUrl, objectName, DateTime.UtcNow.AddMinutes(5))));
    }

    [HttpPost("upload/complete")]
    public async Task<ActionResult<ApiResponse<MediaFileDto>>> CompleteUpload(UploadCompleteRequest request, CancellationToken ct)
    {
        var media = await uow.MediaFiles.GetByIdAsync(request.FileId, ct)
            ?? throw new KeyNotFoundException("Media file not found.");

        ChatMetrics.FileUploadBytesTotal.Inc(media.FileSize ?? 0);
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

        if (string.IsNullOrWhiteSpace(request.ContentType) || !request.ContentType.Contains('/'))
            throw new InvalidOperationException("Invalid content type.");
    }
}
