using ChatApp.Domain.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ChatApp.Infrastructure.Services;

public class PushNotificationService(
    IConfiguration configuration,
    IConnectionMultiplexer redis,
    ILogger<PushNotificationService> logger) : IPushNotificationService
{
    private const int FirebaseBatchSize = 500;
    private static readonly object FirebaseLock = new();
    private static bool _firebaseInitializationAttempted;
    private static FirebaseMessaging? _messaging;
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task SendToUserAsync(Guid userId, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        await SendToUsersAsync(new[] { userId }, title, body, data, ct);
    }

    public async Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var recipients = userIds.Distinct().ToArray();
        if (recipients.Length == 0)
            return;

        var tokens = await GetDeviceTokensAsync(recipients);
        if (tokens.Count == 0)
        {
            logger.LogDebug("No registered push tokens for {RecipientCount} users", recipients.Length);
            return;
        }

        var messaging = GetMessaging();
        if (messaging == null)
        {
            logger.LogInformation("Firebase is not configured. Push notification skipped for {TokenCount} device tokens.", tokens.Count);
            return;
        }

        foreach (var batch in tokens.Chunk(FirebaseBatchSize))
        {
            var message = new MulticastMessage
            {
                Tokens = batch.ToList(),
                Notification = new Notification { Title = title, Body = body },
                Data = data ?? new Dictionary<string, string>()
            };

            var result = await messaging.SendEachForMulticastAsync(message, ct);
            if (result.FailureCount > 0)
            {
                logger.LogWarning("Firebase push completed with {FailureCount}/{TotalCount} failures",
                    result.FailureCount, result.Responses.Count);
            }
        }
    }

    public async Task RegisterDeviceTokenAsync(Guid userId, string fcmToken, string deviceType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fcmToken))
            throw new ArgumentException("FCM token is required.", nameof(fcmToken));

        var normalizedDeviceType = string.IsNullOrWhiteSpace(deviceType) ? "unknown" : deviceType.Trim().ToLowerInvariant();
        await _db.SetAddAsync(DeviceTokenKey(userId), $"{normalizedDeviceType}|{fcmToken.Trim()}");
        logger.LogInformation("Device token registered for user {UserId} ({DeviceType})", userId, normalizedDeviceType);
    }

    public async Task UnregisterDeviceTokenAsync(Guid userId, string fcmToken, CancellationToken ct = default)
    {
        var values = await _db.SetMembersAsync(DeviceTokenKey(userId));
        var matchingValues = values
            .Where(value => TryParseToken(value, out _, out var token) && token == fcmToken)
            .ToArray();

        if (matchingValues.Length > 0)
            await _db.SetRemoveAsync(DeviceTokenKey(userId), matchingValues);

        logger.LogInformation("Device token unregistered for user {UserId}", userId);
    }

    private async Task<List<string>> GetDeviceTokensAsync(IEnumerable<Guid> userIds)
    {
        var tokens = new List<string>();
        foreach (var userId in userIds)
        {
            var values = await _db.SetMembersAsync(DeviceTokenKey(userId));
            foreach (var value in values)
            {
                if (TryParseToken(value, out _, out var token))
                    tokens.Add(token);
            }
        }

        return tokens.Distinct(StringComparer.Ordinal).ToList();
    }

    private FirebaseMessaging? GetMessaging()
    {
        if (_messaging != null || _firebaseInitializationAttempted)
            return _messaging;

        lock (FirebaseLock)
        {
            if (_messaging != null || _firebaseInitializationAttempted)
                return _messaging;

            _firebaseInitializationAttempted = true;
            var projectId = configuration["Firebase:ProjectId"];
            var credentialsPath = configuration["Firebase:CredentialsPath"];

            if (string.IsNullOrWhiteSpace(projectId)
                || string.IsNullOrWhiteSpace(credentialsPath)
                || !File.Exists(credentialsPath)
                || IsPlaceholderCredentials(credentialsPath))
            {
                logger.LogWarning("Firebase credentials are not configured. Push notifications will be stored but not sent.");
                return null;
            }

            try
            {
                FirebaseApp.Create(new AppOptions
                {
                    ProjectId = projectId,
                    Credential = CredentialFactory
                        .FromFile<ServiceAccountCredential>(credentialsPath)
                        .ToGoogleCredential()
                });
                _messaging = FirebaseMessaging.DefaultInstance;
                logger.LogInformation("Firebase messaging initialized for project {ProjectId}", projectId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize Firebase messaging.");
            }

            return _messaging;
        }
    }

    private static bool IsPlaceholderCredentials(string credentialsPath)
    {
        var content = File.ReadAllText(credentialsPath);
        return content.Contains("YOUR_FIREBASE_PROJECT_ID", StringComparison.Ordinal)
            || content.Contains("YOUR_PRIVATE_KEY", StringComparison.Ordinal);
    }

    private static bool TryParseToken(RedisValue value, out string deviceType, out string token)
    {
        var text = value.ToString();
        var delimiterIndex = text.IndexOf('|');
        if (delimiterIndex <= 0 || delimiterIndex == text.Length - 1)
        {
            deviceType = "unknown";
            token = text;
            return !string.IsNullOrWhiteSpace(token);
        }

        deviceType = text[..delimiterIndex];
        token = text[(delimiterIndex + 1)..];
        return !string.IsNullOrWhiteSpace(token);
    }

    private static string DeviceTokenKey(Guid userId) => $"user:{userId}:fcm_tokens";
}
