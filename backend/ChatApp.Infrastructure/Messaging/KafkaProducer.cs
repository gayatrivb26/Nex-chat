using System.Text.Json;
using ChatApp.Application.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Messaging;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IProducer<string, string>? _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration.GetConnectionString("Kafka")
            ?? configuration["ConnectionStrings:Kafka"]
            ?? configuration["Kafka:BootstrapServers"];

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogWarning("Kafka bootstrap servers are not configured. Events will be logged but not published.");
            return;
        }

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageTimeoutMs = 5000
        }).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        if (_producer == null)
        {
            _logger.LogInformation("Kafka disabled. Topic {Topic}, Key {Key}, Payload {Payload}", topic, key, payload);
            return;
        }

        await _producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = payload }, ct);
    }

    public void Dispose() => _producer?.Dispose();
}
