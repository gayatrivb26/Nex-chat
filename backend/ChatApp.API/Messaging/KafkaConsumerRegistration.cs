namespace ChatApp.API.Messaging;

public static class KafkaConsumerRegistration
{
    public static IServiceCollection AddKafkaConsumers(this IServiceCollection services)
    {
        services.AddScoped<DeliveryReceiptKafkaHandler>();
        services.AddScoped<PushNotificationKafkaHandler>();
        services.AddScoped<AnalyticsKafkaHandler>();

        services.AddHostedService<KafkaConsumerHostedService<DeliveryReceiptKafkaHandler>>();
        services.AddHostedService<KafkaConsumerHostedService<PushNotificationKafkaHandler>>();
        services.AddHostedService<KafkaConsumerHostedService<AnalyticsKafkaHandler>>();

        return services;
    }
}
