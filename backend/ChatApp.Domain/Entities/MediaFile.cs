using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class MediaFile : BaseEntity
{
    public Guid? UploadedById { get; private set; }
    public string OriginalName { get; private set; } = string.Empty;
    public string StoredName { get; private set; } = string.Empty;
    public string BucketName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? Checksum { get; set; }
    public bool IsScanned { get; set; }
    public string? ScanResult { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User? UploadedBy { get; set; }

    private MediaFile() { }

    public static MediaFile Create(Guid uploadedById, string originalName,
        string storedName, string bucketName, string filePath, string? mimeType, long? fileSize)
        => new()
        {
            UploadedById = uploadedById,
            OriginalName = originalName,
            StoredName = storedName,
            BucketName = bucketName,
            FilePath = filePath,
            MimeType = mimeType,
            FileSize = fileSize
        };
}
