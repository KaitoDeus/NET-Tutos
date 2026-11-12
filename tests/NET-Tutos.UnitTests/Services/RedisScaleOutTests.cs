using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NET_Tutos.Services;
using StackExchange.Redis;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class RedisScaleOutTests
{
    [Fact]
    public void AddNetTutosRedisScaleOut_InTestingEnvironment_KeepsFileSystemAndInMemoryFallback()
    {
        var services = new ServiceCollection();
        var keysDir = Path.Combine(Path.GetTempPath(), $"nettutos-keys-{Guid.NewGuid():N}");

        try
        {
            var status = services.AddNetTutosRedisScaleOut(
                "localhost:6379,abortConnect=false",
                isTestingEnvironment: true,
                keysDir);

            status.DataProtectionMode.Should().Be("FileSystem");
            status.SignalRBackplane.Should().Be("InMemory");
            status.RedisConfigured.Should().BeTrue();
            services.Any(d => d.ServiceType == typeof(IConnectionMultiplexer)).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(keysDir))
            {
                Directory.Delete(keysDir, recursive: true);
            }
        }
    }

    [Fact]
    public void AddNetTutosRedisScaleOut_WithoutRedis_KeepsFileSystemFallback()
    {
        var services = new ServiceCollection();
        var keysDir = Path.Combine(Path.GetTempPath(), $"nettutos-keys-{Guid.NewGuid():N}");

        try
        {
            var status = services.AddNetTutosRedisScaleOut(null, isTestingEnvironment: false, keysDir);

            status.DataProtectionMode.Should().Be("FileSystem");
            status.SignalRBackplane.Should().Be("InMemory");
            status.RedisConfigured.Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(keysDir))
            {
                Directory.Delete(keysDir, recursive: true);
            }
        }
    }

    [Fact]
    public void AddNetTutosRedisScaleOut_InProductionWithRedis_ConfiguresRedisDataProtectionAndSignalRBackplane()
    {
        var services = new ServiceCollection();
        var keysDir = Path.Combine(Path.GetTempPath(), $"nettutos-keys-{Guid.NewGuid():N}");

        try
        {
            var status = services.AddNetTutosRedisScaleOut(
                "127.0.0.1:6399,abortConnect=false",
                isTestingEnvironment: false,
                keysDir);

            status.DataProtectionMode.Should().Be("Redis");
            status.SignalRBackplane.Should().Be("Redis");
            status.RedisKeyPrefix.Should().Be(RedisScaleOutExtensions.DataProtectionRedisKey);
            status.SignalRChannelPrefix.Should().Be(RedisScaleOutExtensions.SignalRChannelPrefix);
            status.RedisConfigured.Should().BeTrue();

            var options = RedisScaleOutExtensions.ParseResilientRedisOptions("127.0.0.1:6399");
            options.AbortOnConnectFail.Should().BeFalse();
            options.ConnectTimeout.Should().BeGreaterThan(0);

            using var provider = services.BuildServiceProvider();
            var multiplexer = provider.GetService<IConnectionMultiplexer>();
            multiplexer.Should().NotBeNull();
        }
        finally
        {
            if (Directory.Exists(keysDir))
            {
                Directory.Delete(keysDir, recursive: true);
            }
        }
    }
}
