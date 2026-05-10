using ChatApp.Worker.Consumers;
using ChatApp.Worker.HostedServices;
using ChatApp.Infrastructure;
using ChatApp.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Reuse existing infrastructure DI registration
        services.AddInfrastructure(context.Configuration);
        services.AddApplication();

        // Register consumers
        services.AddSingleton<DeliveryReceiptConsumer>();
        services.AddSingleton<PushNotificationConsumer>();
        services.AddSingleton<AnalyticsConsumer>();

        // Hosted service that runs consumers
        services.AddHostedService<KafkaConsumerHostedService>();
    })
    .Build();

await host.RunAsync();
