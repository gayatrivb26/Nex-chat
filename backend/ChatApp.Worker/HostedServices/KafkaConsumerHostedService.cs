using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatApp.Worker.HostedServices;

public class KafkaConsumerHostedService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaConsumerHostedService> _logger;
    private readonly IServiceProvider _services;

    public KafkaConsumerHostedService(IConfiguration configuration, IServiceProvider services, ILogger<KafkaConsumerHostedService> logger)
    {
        _configuration = configuration;
        _services = services;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start separate consumers for each handler type
        _ = Task.Run(() => RunConsumerAsync("messages.sent", "delivery-receipts", async (key, payload, ct) =>
        {
            using var scope = _services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ChatApp.Worker.Consumers.DeliveryReceiptConsumer>();
            await handler.HandleAsync(key, payload, ct);
        }, stoppingToken));

        _ = Task.Run(() => RunConsumerAsync("messages.sent", "push-notifications", async (key, payload, ct) =>
        {
            using var scope = _services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ChatApp.Worker.Consumers.PushNotificationConsumer>();
            await handler.HandleAsync(key, payload, ct);
        }, stoppingToken));

        _ = Task.Run(() => RunConsumerAsync("messages.sent", "analytics", async (key, payload, ct) =>
        {
            using var scope = _services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ChatApp.Worker.Consumers.AnalyticsConsumer>();
            await handler.HandleAsync(key, payload, ct);
        }, stoppingToken));

        return Task.CompletedTask;
    }

    private async Task RunConsumerAsync(string topic, string groupSuffix, Func<string, string, CancellationToken, Task> onMessage, CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration.GetConnectionString("Kafka")
            ?? _configuration["ConnectionStrings:Kafka"]
            ?? _configuration["Kafka:BootstrapServers"];

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogWarning("Kafka not configured, skipping consumer for topic {Topic}", topic);
            return;
        }

        var groupId = ($"nexchat-worker-{groupSuffix}").ToLowerInvariant();

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);

        _logger.LogInformation("Kafka consumer subscribed to {Topic} with group {GroupId}", topic, groupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (cr == null) continue;

                    _logger.LogDebug("Consumed message from {Topic} Key={Key}", topic, cr.Message?.Key);

                    try
                    {
                        await onMessage(cr.Message.Key, cr.Message.Value, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling message from {Topic}", topic);
                    }
                }
                catch (ConsumeException cex)
                {
                    _logger.LogError(cex, "Kafka consume error");
                }
            }
        }
        finally
        {
            try { consumer.Close(); } catch { }
        }
    }
}
