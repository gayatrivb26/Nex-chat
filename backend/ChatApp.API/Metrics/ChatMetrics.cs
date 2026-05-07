using Prometheus;

namespace ChatApp.API.Metrics;

public static class ChatMetrics
{
    public static readonly Gauge ActiveSignalRConnections = Prometheus.Metrics
        .CreateGauge("active_signalr_connections", "Active SignalR connections.");

    public static readonly Counter MessagesSentTotal = Prometheus.Metrics
        .CreateCounter("messages_sent_total", "Messages sent through the real-time path.");

    public static readonly Histogram MessageDeliveryDuration = Prometheus.Metrics
        .CreateHistogram("message_delivery_duration_seconds", "Message delivery duration.");

    public static readonly Gauge ActiveCalls = Prometheus.Metrics
        .CreateGauge("active_calls", "Active calls.");

    public static readonly Counter FileUploadBytesTotal = Prometheus.Metrics
        .CreateCounter("file_upload_bytes_total", "Total uploaded file bytes.");
}
