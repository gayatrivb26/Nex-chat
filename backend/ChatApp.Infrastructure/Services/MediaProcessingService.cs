using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Globalization;

namespace ChatApp.Infrastructure.Services;

public class MediaProcessingService(
    IConfiguration configuration,
    ILogger<MediaProcessingService> logger) : IMediaProcessingService
{
    public async Task<string?> GenerateThumbnailAsync(string sourcePath, string bucket, CancellationToken ct = default)
    {
        if (!File.Exists(sourcePath))
            return null;

        var thumbnailPath = Path.Combine(Path.GetTempPath(), "chatapp-media", $"{Guid.NewGuid():N}.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

        if (IsImagePath(sourcePath))
        {
            using var image = await Image.LoadAsync(sourcePath, ct);
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(480, 480)
            }));

            await image.SaveAsJpegAsync(thumbnailPath, new JpegEncoder { Quality = 82 }, ct);
            return thumbnailPath;
        }

        if (await TryRunFfmpegThumbnailAsync(sourcePath, thumbnailPath, ct))
            return thumbnailPath;

        return null;
    }

    public async Task<(int width, int height)> GetImageDimensionsAsync(Stream imageStream, CancellationToken ct = default)
    {
        if (imageStream.CanSeek)
            imageStream.Position = 0;

        var info = await Image.IdentifyAsync(imageStream, ct)
            ?? throw new InvalidOperationException("Unsupported or corrupt image file.");

        return (info.Width, info.Height);
    }

    public async Task<Stream> CompressImageAsync(Stream imageStream, int maxWidth = 1920, int quality = 85, CancellationToken ct = default)
    {
        if (imageStream.CanSeek)
            imageStream.Position = 0;

        using var image = await Image.LoadAsync(imageStream, ct);
        if (image.Width > maxWidth)
        {
            var targetHeight = (int)Math.Round(image.Height * (maxWidth / (double)image.Width));
            image.Mutate(ctx => ctx.Resize(maxWidth, targetHeight));
        }

        var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = Math.Clamp(quality, 1, 100) }, ct);
        output.Position = 0;
        return output;
    }

    public async Task<int?> GetVideoDurationAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            return null;

        var ffprobePath = configuration["FFmpeg:FFprobePath"] ?? "ffprobe";
        var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"";
        var output = await RunProcessAsync(ffprobePath, args, ct);

        return double.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? (int)Math.Round(seconds)
            : null;
    }

    private async Task<bool> TryRunFfmpegThumbnailAsync(string sourcePath, string thumbnailPath, CancellationToken ct)
    {
        var ffmpegPath = configuration["FFmpeg:Path"] ?? "ffmpeg";
        var args = $"-y -ss 00:00:01 -i \"{sourcePath}\" -frames:v 1 -vf scale='min(480,iw)':-2 \"{thumbnailPath}\"";

        try
        {
            await RunProcessAsync(ffmpegPath, args, ct);
            return File.Exists(thumbnailPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FFmpeg thumbnail generation failed for {SourcePath}", sourcePath);
            return false;
        }
    }

    private async Task<string> RunProcessAsync(string fileName, string arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var output = (await outputTask).Trim();
        if (process.ExitCode == 0)
            return output;

        var error = (await errorTask).Trim();
        throw new InvalidOperationException($"{fileName} exited with {process.ExitCode}: {error}");
    }

    private static bool IsImagePath(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp";
    }
}
