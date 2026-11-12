using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NET_Tutos.BuildingBlocks.Common.Models;
using StackExchange.Redis;

namespace NET_Tutos.YarpGateway.RateLimiting;

/// <summary>
/// Configurable gateway quota settings for sandbox execution and AI Tutor API routes.
/// </summary>
public sealed class DistributedQuotaOptions
{
    public const string SectionName = "RateLimiting";

    public int RunnerPermitLimitPerMinute { get; set; } = 60;
    public int AiTutorPermitLimitPerMinute { get; set; } = 30;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 5;
    public string RedisKeyPrefix { get; set; } = "nettutos:ratelimit:";
}

/// <summary>
/// Diagnostic summary describing active distributed gateway rate-limit policies.
/// </summary>
public sealed record DistributedRateLimitStatusDto(
    string Backend,
    bool RedisConfigured,
    int RunnerPermitLimitPerMinute,
    int AiTutorPermitLimitPerMinute,
    int WindowSeconds,
    string PartitionStrategy,
    IReadOnlyList<string> Policies);

/// <summary>
/// Registers Redis-backed (with in-memory sliding-window fallback) partitioned rate limiting across YARP Gateway replicas.
/// </summary>
public static class DistributedQuotaLimiterExtensions
{
    public const string RunnerPolicyName = "runner-policy";
    public const string AiTutorPolicyName = "aitutor-policy";

    public static DistributedRateLimitStatusDto AddNetTutosDistributedRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var opts = new DistributedQuotaOptions();
        configuration.GetSection(DistributedQuotaOptions.SectionName).Bind(opts);

        opts.RunnerPermitLimitPerMinute = Math.Max(1, opts.RunnerPermitLimitPerMinute);
        opts.AiTutorPermitLimitPerMinute = Math.Max(1, opts.AiTutorPermitLimitPerMinute);
        opts.WindowSeconds = Math.Max(1, opts.WindowSeconds);
        opts.QueueLimit = Math.Max(0, opts.QueueLimit);

        services.AddSingleton(opts);

        string? redisConnection = configuration.GetConnectionString("RedisConnection")
            ?? configuration.GetConnectionString("Redis");
        bool hasRedis = !string.IsNullOrWhiteSpace(redisConnection);
        bool useRedisBackend = hasRedis && !environment.IsEnvironment("Testing");

        if (useRedisBackend)
        {
            var redisOptions = ConfigurationOptions.Parse(redisConnection!, ignoreUnknown: true);
            redisOptions.AbortOnConnectFail = false;
            redisOptions.ConnectTimeout = Math.Min(redisOptions.ConnectTimeout, 1000);
            redisOptions.SyncTimeout = Math.Min(redisOptions.SyncTimeout, 500);
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
        }

        services.AddRateLimiter(rateLimiter =>
        {
            rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiter.OnRejected = async (context, cancellationToken) =>
            {
                int retryAfterSeconds = opts.WindowSeconds;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                }

                context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
                context.HttpContext.Response.ContentType = "application/json";

                var payload = ApiResponse<object>.Fail(
                    $"Rate limit exceeded. Please retry after {retryAfterSeconds}s.");
                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(payload),
                    cancellationToken);
            };

            AddPartitionedPolicy(rateLimiter, RunnerPolicyName, opts.RunnerPermitLimitPerMinute, opts);
            AddPartitionedPolicy(rateLimiter, AiTutorPolicyName, opts.AiTutorPermitLimitPerMinute, opts);
        });

        var status = new DistributedRateLimitStatusDto(
            Backend: useRedisBackend ? "Redis" : "InMemorySlidingWindow",
            RedisConfigured: hasRedis,
            RunnerPermitLimitPerMinute: opts.RunnerPermitLimitPerMinute,
            AiTutorPermitLimitPerMinute: opts.AiTutorPermitLimitPerMinute,
            WindowSeconds: opts.WindowSeconds,
            PartitionStrategy: "ClientIpOrIdentity",
            Policies: [RunnerPolicyName, AiTutorPolicyName]);

        services.AddSingleton(status);
        return status;
    }

    public static IApplicationBuilder UseNetTutosDistributedQuotaHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var opts = context.RequestServices.GetRequiredService<DistributedQuotaOptions>();
            var status = context.RequestServices.GetRequiredService<DistributedRateLimitStatusDto>();
            var policyMatch = ResolveRoutePolicy(context.Request.Path, opts);

            if (policyMatch is null)
            {
                await next();
                return;
            }

            var (policyName, permitLimit) = policyMatch.Value;
            string partitionKey = ResolveClientPartitionKey(context);

            context.Response.Headers["X-RateLimit-Policy"] = policyName;
            context.Response.Headers["X-RateLimit-Limit"] = permitLimit.ToString();
            context.Response.Headers["X-RateLimit-Backend"] = status.Backend;

            var multiplexer = context.RequestServices.GetService<IConnectionMultiplexer>();
            if (multiplexer is { IsConnected: true })
            {
                var (allowed, remaining, retryAfterSeconds) = await TryConsumeRedisQuotaAsync(
                    multiplexer,
                    opts,
                    policyName,
                    partitionKey,
                    permitLimit);

                context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                if (!allowed)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
                    context.Response.ContentType = "application/json";
                    var payload = ApiResponse<object>.Fail(
                        $"Distributed quota exceeded for '{policyName}'. Retry after {retryAfterSeconds}s.");
                    await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                    return;
                }
            }

            await next();
        });
    }

    public static string ResolveClientPartitionKey(HttpContext context)
    {
        string? explicitClient = context.Request.Headers["X-Client-Id"].FirstOrDefault()?.Trim();
        if (!string.IsNullOrEmpty(explicitClient))
        {
            return $"client:{explicitClient}";
        }

        string? userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)?.Trim();
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        string? forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            string firstIp = forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (!string.IsNullOrEmpty(firstIp))
            {
                return $"ip:{firstIp}";
            }
        }

        string? remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrEmpty(remoteIp) ? "ip:local-client" : $"ip:{remoteIp}";
    }

    private static void AddPartitionedPolicy(
        RateLimiterOptions rateLimiter,
        string policyName,
        int permitLimit,
        DistributedQuotaOptions opts)
    {
        rateLimiter.AddPolicy(policyName, context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                ResolveClientPartitionKey(context),
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(opts.WindowSeconds),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = opts.QueueLimit,
                    AutoReplenishment = true
                }));
    }

    private static (string PolicyName, int PermitLimit)? ResolveRoutePolicy(PathString path, DistributedQuotaOptions opts)
    {
        if (path.StartsWithSegments("/api/v1/runner"))
        {
            return (RunnerPolicyName, opts.RunnerPermitLimitPerMinute);
        }

        if (path.StartsWithSegments("/api/v1/aitutor") || path.StartsWithSegments("/api/aitutor"))
        {
            return (AiTutorPolicyName, opts.AiTutorPermitLimitPerMinute);
        }

        return null;
    }

    private static async Task<(bool Allowed, int Remaining, int RetryAfterSeconds)> TryConsumeRedisQuotaAsync(
        IConnectionMultiplexer multiplexer,
        DistributedQuotaOptions opts,
        string policyName,
        string partitionKey,
        int permitLimit)
    {
        try
        {
            long nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long bucket = nowSeconds / opts.WindowSeconds;
            int secondsRemaining = Math.Max(1, (int)(opts.WindowSeconds - (nowSeconds % opts.WindowSeconds)));
            string key = $"{opts.RedisKeyPrefix}{policyName}:{partitionKey}:{bucket}";

            var db = multiplexer.GetDatabase();
            long count = await db.StringIncrementAsync(key);
            if (count == 1)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(opts.WindowSeconds + 5));
            }

            int remaining = Math.Max(0, permitLimit - (int)count);
            return (count <= permitLimit, remaining, secondsRemaining);
        }
        catch
        {
            // Fail open to local partitioned sliding window limiter if Redis temporarily drops
            return (true, permitLimit, opts.WindowSeconds);
        }
    }
}
