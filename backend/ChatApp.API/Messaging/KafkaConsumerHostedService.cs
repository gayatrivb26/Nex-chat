using ChatApp.Application.Interfaces;
using Confluent.Kafka;

namespace ChatApp.API.Messaging;

public sealed class KafkaConsumerHostedService<THandler>(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<KafkaConsumerHostedService<THandler>> logger) : BackgroundService
    where THandler : class, IKafkaMessageHandler
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!bool.TryParse(configuration["Kafka:EnableConsumers"], out var enabled) || !enabled)
        {
            logger.LogInformation("Kafka consumer {Handler} is disabled", typeof(THandler).Name);
            return;
        }

        var bootstrapServers = configuration.GetConnectionString("Kafka")
            ?? configuration["ConnectionStrings:Kafka"]
            ?? configuration["Kafka:BootstrapServers"];

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            logger.LogWarning("Kafka bootstrap servers are not configured. Consumer {Handler} will not start.", typeof(THandler).Name);
            return;
        }

        using var metadataScope = scopeFactory.CreateScope();
        var metadataHandler = metadataScope.ServiceProvider.GetRequiredService<THandler>();
        var topic = metadataHandler.Topic;
        var groupIdSuffix = metadataHandler.GroupIdSuffix;
        var baseGroupId = configuration["Kafka:GroupId"] ?? "chatapp-consumers";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = $"{baseGroupId}.{groupIdSuffix}",
            ClientId = $"{Environment.MachineName}-{groupIdSuffix}",
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            SessionTimeoutMs = 10000,
            MaxPollIntervalMs = 300000
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topic);
        logger.LogInformation("Kafka consumer {Handler} subscribed to {Topic} with group {GroupId}",
            typeof(THandler).Name, topic, consumerConfig.GroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;
            try
            {
                result = consumer.Consume(stoppingToken);
                if (result?.Message == null)
                    continue;

                using var scope = scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                await handler.HandleAsync(result.Message.Key, result.Message.Value, stoppingToken);

                consumer.StoreOffset(result);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error in {Handler}: {Reason}", typeof(THandler).Name, ex.Error.Reason);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (Exception ex) when (result != null)
            {
                logger.LogError(ex, "Kafka handler {Handler} failed at {TopicPartitionOffset}",
                    typeof(THandler).Name, result.TopicPartitionOffset);
                await PublishDeadLetterAsync(result, ex, stoppingToken);
                consumer.StoreOffset(result);
                consumer.Commit(result);
            }
        }

        consumer.Close();
    }

    private async Task PublishDeadLetterAsync(ConsumeResult<string, string> result, Exception exception, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();
        await producer.PublishAsync($"deadletter.{result.Topic}", result.Message.Key ?? Guid.NewGuid().ToString("N"), new
        {
            Handler = typeof(THandler).Name,
            Topic = result.Topic,
            Partition = result.Partition.Value,
            Offset = result.Offset.Value,
            MessageKey = result.Message.Key,
            MessageValue = result.Message.Value,
            Error = exception.Message,
            FailedAt = DateTime.UtcNow
        }, ct);
    }
}
