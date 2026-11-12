using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

namespace NET_Tutos.Services;

/// <summary>
/// Describes the active horizontal scale-out configuration for Web BFF replicas.
/// </summary>
public sealed record ScaleOutTopologyStatus(
    string DataProtectionMode,
    string SignalRBackplane,
    string RedisKeyPrefix,
    string SignalRChannelPrefix,
    bool RedisConfigured);

/// <summary>
/// Configures Redis-backed DataProtection key ring sharing and SignalR backplane
/// when running multi-replica Web pods outside isolated test environments.
/// </summary>
public static class RedisScaleOutExtensions
{
    public const string DataProtectionRedisKey = "nettutos:dataprotection-keys";
    public const string SignalRChannelPrefix = "nettutos:signalr:";

    /// <summary>
    /// Registers SignalR and DataProtection with automatic Redis scale-out when Redis is configured
    /// outside the Testing environment, falling back cleanly to local filesystem keys otherwise.
    /// </summary>
    public static ScaleOutTopologyStatus AddNetTutosRedisScaleOut(
        this IServiceCollection services,
        string? redisConnectionString,
        bool isTestingEnvironment,
        string keysFolder)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(keysFolder);

        Directory.CreateDirectory(keysFolder);

        var signalRBuilder = services.AddSignalR();
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("NET-Tutos")
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

        bool hasRedis = !string.IsNullOrWhiteSpace(redisConnectionString);
        bool enableRedisScaleOut = hasRedis && !isTestingEnvironment;

        if (enableRedisScaleOut)
        {
            var dpOptions = ParseResilientRedisOptions(redisConnectionString!);
            var multiplexer = ConnectionMultiplexer.Connect(dpOptions);
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);

            dataProtectionBuilder.PersistKeysToStackExchangeRedis(
                () => multiplexer.GetDatabase(),
                DataProtectionRedisKey);

            signalRBuilder.AddStackExchangeRedis(options =>
            {
                var signalROptions = ParseResilientRedisOptions(redisConnectionString!);
                signalROptions.ChannelPrefix = RedisChannel.Literal(SignalRChannelPrefix);
                options.Configuration = signalROptions;
            });
        }

        var status = new ScaleOutTopologyStatus(
            DataProtectionMode: enableRedisScaleOut ? "Redis" : "FileSystem",
            SignalRBackplane: enableRedisScaleOut ? "Redis" : "InMemory",
            RedisKeyPrefix: DataProtectionRedisKey,
            SignalRChannelPrefix: SignalRChannelPrefix,
            RedisConfigured: hasRedis);

        services.AddSingleton(status);
        return status;
    }

    public static ConfigurationOptions ParseResilientRedisOptions(string redisConnectionString)
    {
        var options = ConfigurationOptions.Parse(redisConnectionString, ignoreUnknown: true);
        // Never abort container startup if Redis is still warming up in Kubernetes.
        options.AbortOnConnectFail = false;
        if (options.ConnectTimeout <= 0)
        {
            options.ConnectTimeout = 2000;
        }
        if (options.SyncTimeout <= 0)
        {
            options.SyncTimeout = 2000;
        }
        return options;
    }
}
