using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nClam;

namespace ChatApp.Infrastructure.Services;

public class VirusScanService(
    IConfiguration configuration,
    ILogger<VirusScanService> logger) : IVirusScanService
{
    public async Task<(bool isClean, string result)> ScanAsync(Stream fileStream, CancellationToken ct = default)
    {
        if (!IsEnabled())
            return (true, "skipped");

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        var result = await CreateClient().SendAndScanFileAsync(fileStream, ct);
        return MapResult(result);
    }

    public async Task<(bool isClean, string result)> ScanByPathAsync(string filePath, CancellationToken ct = default)
    {
        if (!IsEnabled())
            return (true, "skipped");

        if (!File.Exists(filePath))
            return (false, "file_not_found");

        var result = await CreateClient().SendAndScanFileAsync(filePath, ct);
        return MapResult(result);
    }

    private bool IsEnabled()
        => bool.TryParse(configuration["ClamAv:Enabled"], out var enabled) && enabled;

    private ClamClient CreateClient()
    {
        var host = configuration["ClamAv:Host"] ?? "localhost";
        var port = int.TryParse(configuration["ClamAv:Port"], out var configuredPort) ? configuredPort : 3310;
        var maxStreamSize = long.TryParse(configuration["ClamAv:MaxStreamSizeBytes"], out var configuredMax)
            ? configuredMax
            : 2L * 1024 * 1024 * 1024;

        return new ClamClient(host, port)
        {
            MaxStreamSize = maxStreamSize
        };
    }

    private (bool isClean, string result) MapResult(ClamScanResult scanResult)
    {
        return scanResult.Result switch
        {
            ClamScanResults.Clean => (true, "clean"),
            ClamScanResults.VirusDetected => (false,
                scanResult.InfectedFiles?.FirstOrDefault()?.VirusName ?? "virus_detected"),
            _ => MapScanError(scanResult)
        };
    }

    private (bool isClean, string result) MapScanError(ClamScanResult scanResult)
    {
        logger.LogWarning("ClamAV scan failed: {Result}", scanResult.RawResult);
        return (false, $"scan_error:{scanResult.RawResult}");
    }
}
