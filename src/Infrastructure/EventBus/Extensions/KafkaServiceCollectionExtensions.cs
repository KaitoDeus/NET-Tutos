using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;

namespace NET_Tutos.BuildingBlocks.EventBus.Extensions;

public static class KafkaServiceCollectionExtensions
{
    /// <summary>
    /// Registers Apache Kafka Event Streaming services including resilient producer,
    /// environment-aware configuration binding, and circuit-breaker telemetry.
    /// </summary>
    public static IServiceCollection AddKafkaEventStreaming(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(options =>
        {
            var section = configuration.GetSection(KafkaOptions.SectionName);
            if (section.Exists())
            {
                section.Bind(options);
            }

            var envServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
            if (!string.IsNullOrWhiteSpace(envServers))
            {
                options.BootstrapServers = envServers.Trim();
            }

            var envEnabled = Environment.GetEnvironmentVariable("KAFKA_ENABLED");
            if (bool.TryParse(envEnabled, out var enabled))
            {
                options.Enabled = enabled;
            }
        });

        services.AddSingleton<IKafkaProducer, KafkaProducer>();

        return services;
    }
}
