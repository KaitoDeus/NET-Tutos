using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NET_Tutos.Services;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class HybridRedisCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_PopulatesL1AndL2_AndPreventsCacheStampede()
    {
        // Arrange
        using var l1 = new MemoryCache(new MemoryCacheOptions());
        var l2 = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheService = new HybridRedisCacheService(l1, l2);

        int factoryInvocations = 0;

        // Act: 20 concurrent callers requesting the exact same cache key
        var tasks = Enumerable.Range(0, 20).Select(_ =>
            cacheService.GetOrCreateAsync(
                "stampede:test-key",
                async ct =>
                {
                    Interlocked.Increment(ref factoryInvocations);
                    await Task.Delay(30, ct);
                    return "CachedPayload-2026";
                },
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(2)));

        var results = await Task.WhenAll(tasks);

        // Assert: Only 1 factory execution occurred (19 others hit L1 after lock release)
        factoryInvocations.Should().Be(1);
        results.Should().AllBe("CachedPayload-2026");

        var diag = await cacheService.GetDiagnosticsAsync();
        diag.CacheMisses.Should().Be(1);
        diag.L1MemoryHits.Should().Be(19);
    }

    [Fact]
    public async Task GetAsync_WhenL1Evicted_ReadsFromL2RedisAndBackfillsL1()
    {
        // Arrange
        using var l1 = new MemoryCache(new MemoryCacheOptions());
        var l2 = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheService = new HybridRedisCacheService(l1, l2);

        await cacheService.SetAsync("l2:backfill-key", new { Title = "Redis L2 Hit", Score = 100 });

        // Evict from L1 only to simulate another web container reading from shared Redis L2
        l1.Remove("l2:backfill-key");

        // Act
        var fromL2 = await cacheService.GetAsync<Dictionary<string, object>>("l2:backfill-key");

        // Assert
        fromL2.Should().NotBeNull();
        var diag = await cacheService.GetDiagnosticsAsync();
        diag.L2RedisHits.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_CachesDeterministicCodeInHybridCache()
    {
        // Arrange
        using var l1 = new MemoryCache(new MemoryCacheOptions());
        var l2 = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheService = new HybridRedisCacheService(l1, l2);
        var codeService = new CodeExecutionService(cacheService: cacheService);

        string code = "Console.WriteLine(40 + 2);";

        // Act 1: Cold execution (compiles with Roslyn and writes to cache)
        var firstRun = await codeService.ExecuteAsync(code);
        // Act 2: Warm execution (served from L1/L2 cache)
        var secondRun = await codeService.ExecuteAsync(code);

        // Assert
        firstRun.IsSuccess.Should().BeTrue();
        firstRun.Output.Trim().Should().Be("42");
        secondRun.IsSuccess.Should().BeTrue();
        secondRun.Output.Trim().Should().Be("42");

        var diag = await cacheService.GetDiagnosticsAsync();
        diag.TotalWrites.Should().BeGreaterThanOrEqualTo(1);
        diag.L1MemoryHits.Should().BeGreaterThanOrEqualTo(1);
    }
}
