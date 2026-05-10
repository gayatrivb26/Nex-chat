namespace ChatApp.API.Messaging;

public interface IKafkaMessageHandler
{
    string Topic { get; }
    string GroupIdSuffix { get; }
    Task HandleAsync(string key, string payload, CancellationToken ct);
}
