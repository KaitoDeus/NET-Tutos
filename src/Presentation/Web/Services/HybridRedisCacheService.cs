using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace NET_Tutos.Services;

public class CacheTelemetrySnapshot
{
    public string ActiveProvider { get; set; } = string.Empty;
    public bool IsRedisConfigured { get; set; }
    public bool IsRedisHealthy { get; set; }
    public long L1MemoryHits { get; set; }
    public long L2RedisHits { get; set; }
    public long CacheMisses { get; set; }
    public long TotalWrites { get; set; }
    public long RedisFallbackCount { get; set; }
    public double HitRatioPercent { get; set; }
}

public interface IAppCacheService
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? l2Expiration = null,
        TimeSpan? l1Expiration = null,
        CancellationToken cancellationToken = default);

    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? l2Expiration = null, TimeSpan? l1Expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<CacheTelemetrySnapshot> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Kiến trúc Hybrid Cache 2 tầng (Two-Tier L1 In-Memory + L2 Redis Distributed Cache):
/// - L1 (IMemoryCache): Truy xuất cực nhanh trong RAM của tiến trình (~0.005ms).
/// - L2 (IDistributedCache / Redis 7): Chia sẻ bộ nhớ đệm phân tán giữa nhiều container Web/Microservices (~0.5-1ms).
/// - Chống Cache Stampede (Thundering Herd) bằng khóa SemaphoreSlim theo từng Key (Double-Checked Locking).
/// - Tự động Circuit-Breaker Fallback về L1 nếu Redis container tạm ngắt kết nối.
/// </summary>
public class HybridRedisCacheService : IAppCacheService
{
    private readonly IMemoryCache _l1MemoryCache;
    private readonly IDistributedCache _l2DistributedCache;
    private readonly ILogger<HybridRedisCacheService>? _logger;
    private readonly bool _isRedisConfigured;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private long _l1Hits;
    private long _l2Hits;
    private long _misses;
    private long _writes;
    private long _redisFallbacks;
    private DateTime _redisCircuitOpenUntilUtc = DateTime.MinValue;

    public HybridRedisCacheService(
        IMemoryCache l1MemoryCache,
        IDistributedCache l2DistributedCache,
        IConfiguration? configuration = null,
        ILogger<HybridRedisCacheService>? logger = null)
    {
        _l1MemoryCache = l1MemoryCache;
        _l2DistributedCache = l2DistributedCache;
        _logger = logger;

        var redisConn = configuration?.GetConnectionString("RedisConnection")
                     ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION");
        _isRedisConfigured = !string.IsNullOrWhiteSpace(redisConn);
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? l2Expiration = null,
        TimeSpan? l1Expiration = null,
        CancellationToken cancellationToken = default)
    {
        // Bước 1: Kiểm tra tầng L1 (In-Memory Cache)
        if (_l1MemoryCache.TryGetValue(key, out T? l1Value) && l1Value is not null)
        {
            Interlocked.Increment(ref _l1Hits);
            return l1Value;
        }

        // Bước 2: Kiểm tra tầng L2 (Redis Distributed Cache)
        var l2Value = await TryGetFromL2Async<T>(key, cancellationToken);
        if (l2Value is not null)
        {
            Interlocked.Increment(ref _l2Hits);
            var effectiveL1Ttl = l1Expiration ?? TimeSpan.FromMinutes(2);
            _l1MemoryCache.Set(key, l2Value, effectiveL1Ttl);
            return l2Value;
        }

        // Bước 3: Chống Cache Stampede (chỉ 1 luồng truy vấn DB/Roslyn cho cùng 1 key)
        var keyLock = KeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken);
        try
        {
            // Double-checked locking: kiểm tra lại L1 sau khi giành được khóa
            if (_l1MemoryCache.TryGetValue(key, out l1Value) && l1Value is not null)
            {
                Interlocked.Increment(ref _l1Hits);
                return l1Value;
            }

            Interlocked.Increment(ref _misses);
            var created = await factory(cancellationToken);
            if (created is not null)
            {
                await SetAsync(key, created, l2Expiration, l1Expiration, cancellationToken);
            }
            return created;
        }
        finally
        {
            keyLock.Release();
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_l1MemoryCache.TryGetValue(key, out T? l1Value) && l1Value is not null)
        {
            Interlocked.Increment(ref _l1Hits);
            return l1Value;
        }

        var l2Value = await TryGetFromL2Async<T>(key, cancellationToken);
        if (l2Value is not null)
        {
            Interlocked.Increment(ref _l2Hits);
            _l1MemoryCache.Set(key, l2Value, TimeSpan.FromMinutes(2));
            return l2Value;
        }

        Interlocked.Increment(ref _misses);
        return default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? l2Expiration = null,
        TimeSpan? l1Expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (value is null) return;

        var effectiveL2 = l2Expiration ?? TimeSpan.FromMinutes(15);
        var effectiveL1 = l1Expiration ?? TimeSpan.FromMinutes(Math.Min(5, effectiveL2.TotalMinutes));

        _l1MemoryCache.Set(key, value, effectiveL1);
        Interlocked.Increment(ref _writes);

        if (DateTime.UtcNow < _redisCircuitOpenUntilUtc)
        {
            return;
        }

        try
        {
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = effectiveL2
            };
            await _l2DistributedCache.SetAsync(key, jsonBytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            TripRedisCircuit(ex, key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _l1MemoryCache.Remove(key);

        if (DateTime.UtcNow < _redisCircuitOpenUntilUtc)
        {
            return;
        }

        try
        {
            await _l2DistributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            TripRedisCircuit(ex, key);
        }
    }

    public async Task<CacheTelemetrySnapshot> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        bool redisHealthy = false;
        if (DateTime.UtcNow >= _redisCircuitOpenUntilUtc)
        {
            try
            {
                const string probeKey = "diagnostics:health-probe";
                await _l2DistributedCache.SetStringAsync(
                    probeKey,
                    "ok",
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
                    cancellationToken);
                var val = await _l2DistributedCache.GetStringAsync(probeKey, cancellationToken);
                redisHealthy = val == "ok";
            }
            catch
            {
                redisHealthy = false;
            }
        }

        long l1 = Interlocked.Read(ref _l1Hits);
        long l2 = Interlocked.Read(ref _l2Hits);
        long misses = Interlocked.Read(ref _misses);
        long totalLookups = l1 + l2 + misses;
        double hitRatio = totalLookups > 0
            ? Math.Round((double)(l1 + l2) / totalLookups * 100.0, 2)
            : 100.0;

        return new CacheTelemetrySnapshot
        {
            ActiveProvider = _isRedisConfigured && redisHealthy
                ? "Hybrid L1 (IMemoryCache) + L2 (StackExchange.Redis 7)"
                : "Hybrid L1 (IMemoryCache) + L2 (Distributed In-Memory Fallback)",
            IsRedisConfigured = _isRedisConfigured,
            IsRedisHealthy = redisHealthy,
            L1MemoryHits = l1,
            L2RedisHits = l2,
            CacheMisses = misses,
            TotalWrites = Interlocked.Read(ref _writes),
            RedisFallbackCount = Interlocked.Read(ref _redisFallbacks),
            HitRatioPercent = hitRatio
        };
    }

    public static string ComputeSha256Hash(string rawInput)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawInput.Trim()));
        return Convert.ToHexStringLower(bytes);
    }

    private async Task<T?> TryGetFromL2Async<T>(string key, CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow < _redisCircuitOpenUntilUtc)
        {
            return default;
        }

        try
        {
            var bytes = await _l2DistributedCache.GetAsync(key, cancellationToken);
            if (bytes is null || bytes.Length == 0)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
        }
        catch (Exception ex)
        {
            TripRedisCircuit(ex, key);
            return default;
        }
    }

    private void TripRedisCircuit(Exception ex, string key)
    {
        Interlocked.Increment(ref _redisFallbacks);
        _redisCircuitOpenUntilUtc = DateTime.UtcNow.AddSeconds(30);
        _logger?.LogWarning(ex, "Redis L2 cache unavailable for key '{CacheKey}'. Circuit open for 30s; serving seamlessly from L1 MemoryCache.", key);
    }
}
