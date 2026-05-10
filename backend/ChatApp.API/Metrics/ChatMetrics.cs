using Prometheus;

namespace ChatApp.API.Metrics;

public static class ChatMetrics
{
    public static readonly Gauge ActiveSignalRConnections = Prometheus.Metrics
        .CreateGauge("active_signalr_connections", "Number of active SignalR connections.");

    public static readonly Counter MessagesSentTotal = Prometheus.Metrics
        .CreateCounter("messages_sent_total", "Total messages sent via the real-time path.",
            new CounterConfiguration { LabelNames = new[] { "conversation_type" } });

    public static readonly Histogram MessageDeliveryDuration = Prometheus.Metrics
        .CreateHistogram("message_delivery_duration_seconds",
            "Time from message send to delivery confirmation.",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });

    public static readonly Gauge ActiveCalls = Prometheus.Metrics
        .CreateGauge("active_calls", "Number of currently active audio/video calls.",
            new GaugeConfiguration { LabelNames = new[] { "call_type" } });

    public static readonly Counter FileUploadBytesTotal = Prometheus.Metrics
        .CreateCounter("file_upload_bytes_total", "Total bytes uploaded.",
            new CounterConfiguration { LabelNames = new[] { "mime_category" } });

    public static readonly Counter AuthEventsTotal = Prometheus.Metrics
        .CreateCounter("auth_events_total", "Authentication events.",
            new CounterConfiguration { LabelNames = new[] { "event_type" } }); // login_ok, login_fail, register, otp_sent

    public static readonly Gauge OnlineUsers = Prometheus.Metrics
        .CreateGauge("online_users_total", "Number of users currently online.");

    public static readonly Counter KafkaEventsPublished = Prometheus.Metrics
        .CreateCounter("kafka_events_published_total", "Total Kafka events published.",
            new CounterConfiguration { LabelNames = new[] { "topic" } });
}
