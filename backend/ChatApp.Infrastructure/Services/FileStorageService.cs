using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace ChatApp.Infrastructure.Services;

public class FileStorageService(
    IMinioClient minio,
    IConfiguration configuration,
    ILogger<FileStorageService> logger) : IFileStorageService
{
    private static readonly SemaphoreSlim BucketCreationLock = new(1, 1);
    private readonly IMinioClient _presignClient = CreatePresignClient(configuration, minio);

    public async Task<(string objectName, string filePath)> UploadAsync(
        Stream fileStream, string fileName, string contentType,
        string bucket, CancellationToken ct = default)
    {
        if (!fileStream.CanSeek)
            throw new InvalidOperationException("Upload stream must be seekable so object size can be verified.");

        await EnsureBucketExistsAsync(bucket, ct);

        var objectName = BuildObjectName(fileName);
        fileStream.Position = 0;

        var args = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await minio.PutObjectAsync(args, ct);
        return (objectName, $"{bucket}/{objectName}");
    }

    public async Task<string> GetPresignedUrlAsync(string bucket, string objectName,
        int expiryMinutes = 60, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(bucket, ct);
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithExpiry(ToExpirySeconds(expiryMinutes));

        return await _presignClient.PresignedGetObjectAsync(args);
    }

    public async Task<string> GetPresignedUploadUrlAsync(string bucket, string objectName,
        int expiryMinutes = 5, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(bucket, ct);
        var args = new PresignedPutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithExpiry(ToExpirySeconds(expiryMinutes));

        return await _presignClient.PresignedPutObjectAsync(args);
    }

    public async Task DeleteAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName);

        await minio.RemoveObjectAsync(args, ct);
    }

    public async Task<bool> ObjectExistsAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName);
            await minio.StatObjectAsync(args, ct);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (BucketNotFoundException)
        {
            return false;
        }
    }

    public async Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        var output = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(output));

        await minio.GetObjectAsync(args, ct);
        output.Position = 0;
        return output;
    }

    public async Task DownloadToFileAsync(string bucket, string objectName, string destinationPath, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        var args = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithFile(destinationPath);

        await minio.GetObjectAsync(args, ct);
    }

    private async Task EnsureBucketExistsAsync(string bucket, CancellationToken ct)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(bucket);
        if (await minio.BucketExistsAsync(existsArgs, ct))
            return;

        await BucketCreationLock.WaitAsync(ct);
        try
        {
            if (await minio.BucketExistsAsync(existsArgs, ct))
                return;

            await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
            logger.LogInformation("Created MinIO bucket {Bucket}", bucket);
        }
        finally
        {
            BucketCreationLock.Release();
        }
    }

    private static string BuildObjectName(string fileName)
    {
        var safeName = SanitizeFileName(fileName);
        return $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeName}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
            name = "upload.bin";

        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        const int maxStoredFileNameLength = 180;
        if (name.Length <= maxStoredFileNameLength)
            return name;

        var extension = Path.GetExtension(name);
        var stem = Path.GetFileNameWithoutExtension(name);
        var maxStemLength = Math.Max(1, maxStoredFileNameLength - extension.Length);
        return stem[..Math.Min(stem.Length, maxStemLength)] + extension;
    }

    private static int ToExpirySeconds(int expiryMinutes)
        => Math.Clamp(expiryMinutes, 1, 7 * 24 * 60) * 60;

    private static IMinioClient CreatePresignClient(IConfiguration configuration, IMinioClient fallbackClient)
    {
        var publicUrl = configuration["MinIO:PublicUrl"];
        if (string.IsNullOrWhiteSpace(publicUrl) || !Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri))
            return fallbackClient;

        var accessKey = configuration["MinIO:AccessKey"];
        var secretKey = configuration["MinIO:SecretKey"];
        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            return fallbackClient;

        return new MinioClient()
            .WithEndpoint(uri.Authority)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            .Build();
    }
}
