using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NET_Tutos.Services;

public sealed record NeuralEmbeddingResult(float[] Vector, string ModelName, bool IsLiveGemini);

public interface INeuralEmbeddingService
{
    int NeuralDimensions { get; }
    string ActiveModelName { get; }
    Task<NeuralEmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NeuralEmbeddingResult>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}

/// <summary>
/// Produces L2-normalized 768-dimensional dense neural embeddings using Google Gemini
/// batch/single embedding APIs when configured and reachable, with automatic cooldown circuit breaker
/// and deterministic multilingual EN/VI semantic projection fallback when offline or in CI.
/// </summary>
public sealed class GeminiNeuralEmbeddingService : INeuralEmbeddingService
{
    public const int DefaultDimensions = 768;
    public const string FallbackModelName = "nettutos-neural-hash-768";

    private readonly IConfiguration? _configuration;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<GeminiNeuralEmbeddingService>? _logger;
    private DateTime _geminiCooldownUntilUtc = DateTime.MinValue;
    private string? _resolvedLiveModel;

    private static readonly string[][] MultilingualConceptClusters =
    [
        ["async", "await", "task", "valuetask", "threadpool", "concurrency", "statemachine", "bat", "dong", "bo", "da", "luong", "tinh", "cancellationtoken"],
        ["linq", "ienumerable", "iqueryable", "deferred", "projection", "select", "where", "groupby", "truy", "van", "tri", "hoan"],
        ["ef", "efcore", "entity", "framework", "dbcontext", "asnotracking", "assplitquery", "n+1", "migration", "co", "so", "du", "lieu", "postgresql", "sql"],
        ["dependency", "injection", "di", "scoped", "transient", "singleton", "captive", "serviceprovider", "iservicecollection", "vong", "doi", "tiem", "phu", "thuoc"],
        ["delegate", "action", "func", "predicate", "event", "lambda", "closure", "anonymous", "uy", "quyen", "su", "kien"],
        ["gc", "garbage", "collector", "heap", "stack", "boxing", "unboxing", "span", "memory", "valuetype", "referencetype", "bo", "nho", "cap", "phat"],
        ["microservice", "microservices", "yarp", "gateway", "masstransit", "rabbitmq", "kafka", "grpc", "outbox", "dlq", "polly", "resilience", "phan", "tan", "vi", "dich", "vu"],
        ["redis", "cache", "caching", "hybrid", "distributed", "stampede", "bo", "nho", "dem"],
        ["jwt", "oauth", "auth", "authentication", "authorization", "rbac", "csrf", "antiforgery", "security", "bao", "mat", "xac", "thuc"],
        ["aspnet", "mvc", "minimal", "api", "middleware", "kestrel", "routing", "controller", "razor", "signalr", "web"],
        ["oop", "inheritance", "polymorphism", "encapsulation", "abstraction", "interface", "record", "abstract", "ke", "thua", "da", "hinh", "dong", "goi", "truu", "tuong"],
        ["algorithm", "bigo", "complexity", "sorting", "search", "binary", "hashmap", "dictionary", "stack", "queue", "thuat", "toan", "do", "phuc", "tap"],
        ["xunit", "testing", "unit", "integration", "mock", "moq", "fluentassertions", "tdd", "kiem", "thu", "don", "vi"]
    ];

    public GeminiNeuralEmbeddingService(
        IConfiguration? configuration = null,
        IHttpClientFactory? httpClientFactory = null,
        ILogger<GeminiNeuralEmbeddingService>? logger = null)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public int NeuralDimensions => DefaultDimensions;

    private string? ApiKey =>
        _configuration?["Gemini:ApiKey"]
        ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
        ?? Environment.GetEnvironmentVariable("Gemini__ApiKey");

    private bool IsLiveGeminiEnabled =>
        (_configuration?.GetValue<bool>("Gemini:Enabled", true) ?? false)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && _httpClientFactory != null
        && DateTime.UtcNow >= _geminiCooldownUntilUtc;

    private string ConfiguredEmbeddingModel =>
        _configuration?["Gemini:EmbeddingModel"] ?? "gemini-embedding-001";

    public string ActiveModelName =>
        IsLiveGeminiEnabled
            ? $"gemini:{_resolvedLiveModel ?? ConfiguredEmbeddingModel}"
            : FallbackModelName;

    public async Task<NeuralEmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new NeuralEmbeddingResult(new float[DefaultDimensions], ActiveModelName, false);
        }

        if (IsLiveGeminiEnabled)
        {
            var candidates = GetCandidateModels();
            foreach (var model in candidates)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(6));

                    var client = _httpClientFactory!.CreateClient("Gemini");
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent?key={Uri.EscapeDataString(ApiKey!)}";
                    var payload = new
                    {
                        model = $"models/{model}",
                        content = new { parts = new[] { new { text = text.Length > 4000 ? text[..4000] : text } } },
                        outputDimensionality = DefaultDimensions
                    };

                    using var response = await client.PostAsJsonAsync(url, payload, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
                        if (TryParseVector(doc.RootElement, out var vec))
                        {
                            _resolvedLiveModel = model;
                            return new NeuralEmbeddingResult(vec, $"gemini:{model}", true);
                        }
                    }
                    else
                    {
                        _logger?.LogDebug("Gemini embedding model {Model} returned HTTP {Code}", model, (int)response.StatusCode);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogDebug("Gemini embedding call for {Model} failed: {Msg}", model, ex.Message);
                }
            }

            // Trip short cooldown so offline/rate-limited environments skip per-item retries immediately
            _geminiCooldownUntilUtc = DateTime.UtcNow.AddMinutes(3);
        }

        return new NeuralEmbeddingResult(ComputeMultilingualDenseProjection(text), FallbackModelName, false);
    }

    public async Task<IReadOnlyList<NeuralEmbeddingResult>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0) return Array.Empty<NeuralEmbeddingResult>();

        var results = new NeuralEmbeddingResult[texts.Count];

        if (IsLiveGeminiEnabled)
        {
            var candidates = GetCandidateModels();
            foreach (var model in candidates)
            {
                try
                {
                    const int batchSize = 80;
                    bool allBatchesSucceeded = true;

                    for (int offset = 0; offset < texts.Count; offset += batchSize)
                    {
                        int count = Math.Min(batchSize, texts.Count - offset);
                        var chunk = texts.Skip(offset).Take(count).ToList();

                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(TimeSpan.FromSeconds(12));

                        var client = _httpClientFactory!.CreateClient("Gemini");
                        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:batchEmbedContents?key={Uri.EscapeDataString(ApiKey!)}";
                        var payload = new
                        {
                            requests = chunk.Select(t => new
                            {
                                model = $"models/{model}",
                                content = new { parts = new[] { new { text = string.IsNullOrWhiteSpace(t) ? " " : (t.Length > 4000 ? t[..4000] : t) } } },
                                outputDimensionality = DefaultDimensions
                            }).ToArray()
                        };

                        using var response = await client.PostAsJsonAsync(url, payload, cts.Token);
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger?.LogDebug("Gemini batchEmbedContents for {Model} returned HTTP {Code}", model, (int)response.StatusCode);
                            allBatchesSucceeded = false;
                            break;
                        }

                        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
                        if (!doc.RootElement.TryGetProperty("embeddings", out var embs) || embs.ValueKind != JsonValueKind.Array)
                        {
                            allBatchesSucceeded = false;
                            break;
                        }

                        int i = 0;
                        foreach (var item in embs.EnumerateArray())
                        {
                            if (i >= count) break;
                            if (TryParseVectorFromValuesContainer(item, out var vec))
                            {
                                results[offset + i] = new NeuralEmbeddingResult(vec, $"gemini:{model}", true);
                            }
                            else
                            {
                                allBatchesSucceeded = false;
                                break;
                            }
                            i++;
                        }

                        if (!allBatchesSucceeded || i != count)
                        {
                            allBatchesSucceeded = false;
                            break;
                        }
                    }

                    if (allBatchesSucceeded)
                    {
                        _resolvedLiveModel = model;
                        return results;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogDebug("Gemini batch embedding failed for {Model}: {Msg}", model, ex.Message);
                }
            }

            _geminiCooldownUntilUtc = DateTime.UtcNow.AddMinutes(3);
        }

        for (int i = 0; i < texts.Count; i++)
        {
            results[i] = new NeuralEmbeddingResult(ComputeMultilingualDenseProjection(texts[i]), FallbackModelName, false);
        }
        return results;
    }

    private IEnumerable<string> GetCandidateModels()
    {
        if (!string.IsNullOrEmpty(_resolvedLiveModel))
        {
            yield return _resolvedLiveModel;
            yield break;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in new[] { ConfiguredEmbeddingModel, "gemini-embedding-001", "text-embedding-004" })
        {
            if (!string.IsNullOrWhiteSpace(m) && seen.Add(m.Trim()))
            {
                yield return m.Trim();
            }
        }
    }

    private static bool TryParseVector(JsonElement root, out float[] vec)
    {
        if (root.TryGetProperty("embedding", out var emb))
        {
            return TryParseVectorFromValuesContainer(emb, out vec);
        }
        vec = Array.Empty<float>();
        return false;
    }

    private static bool TryParseVectorFromValuesContainer(JsonElement container, out float[] vec)
    {
        if (container.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array)
        {
            vec = new float[DefaultDimensions];
            int idx = 0;
            foreach (var v in values.EnumerateArray())
            {
                if (idx >= DefaultDimensions) break;
                vec[idx++] = v.GetSingle();
            }
            if (idx > 0)
            {
                NormalizeInPlace(vec);
                return true;
            }
        }
        vec = Array.Empty<float>();
        return false;
    }

    public static float[] ComputeMultilingualDenseProjection(string text)
    {
        var vec = new float[DefaultDimensions];
        if (string.IsNullOrWhiteSpace(text)) return vec;

        string normalized = FoldDiacritics(text.ToLowerInvariant());
        var words = ExtractTokens(normalized);

        foreach (var token in words)
        {
            for (int c = 0; c < MultilingualConceptClusters.Length; c++)
            {
                if (Array.IndexOf(MultilingualConceptClusters[c], token) >= 0)
                {
                    int baseSlot = c * 9;
                    for (int k = 0; k < 8; k++)
                    {
                        int slot = (baseSlot + k) % 128;
                        vec[slot] += 2.25f;
                    }
                }
            }
        }

        const int projStart = 128;
        const int projSpan = DefaultDimensions - projStart;

        void ProjectFeature(string feature, float weight)
        {
            uint h1 = Fnv1aHash(feature);
            uint h2 = Fnv1aHash(feature + "#2");
            int bucket1 = projStart + (int)(h1 % (uint)projSpan);
            int bucket2 = projStart + (int)(h2 % (uint)projSpan);
            float sign1 = (h1 & 1) == 0 ? 1f : -1f;
            float sign2 = (h2 & 1) == 0 ? 1f : -1f;
            vec[bucket1] += weight * sign1;
            vec[bucket2] += weight * 0.7f * sign2;
        }

        foreach (var w in words)
        {
            if (w.Length < 2) continue;
            ProjectFeature("w:" + w, 1.5f);
            if (w.Length >= 4)
            {
                string padded = $"^{w}$";
                for (int i = 0; i + 3 <= padded.Length; i++)
                {
                    ProjectFeature("g:" + padded.Substring(i, 3), 0.45f);
                }
            }
        }

        for (int i = 0; i + 1 < words.Count; i++)
        {
            ProjectFeature($"b:{words[i]}_{words[i + 1]}", 1.85f);
        }

        NormalizeInPlace(vec);
        return vec;
    }

    public static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        int len = Math.Min(a.Count, b.Count);
        if (len == 0) return 0;

        double dot = 0;
        double magA = 0;
        double magB = 0;
        for (int i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA <= 1e-12 || magB <= 1e-12) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    public static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public static string ToPgVectorLiteral(float[] vector)
    {
        var sb = new StringBuilder(vector.Length * 7);
        sb.Append('[');
        for (int i = 0; i < vector.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(vector[i].ToString("G6", CultureInfo.InvariantCulture));
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static void NormalizeInPlace(float[] vec)
    {
        double sumSq = 0;
        for (int i = 0; i < vec.Length; i++) sumSq += vec[i] * vec[i];
        if (sumSq <= 1e-12) return;

        float invNorm = (float)(1.0 / Math.Sqrt(sumSq));
        for (int i = 0; i < vec.Length; i++) vec[i] *= invNorm;
    }

    private static List<string> ExtractTokens(string foldedLower)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        foreach (char c in foldedLower)
        {
            if (char.IsLetterOrDigit(c) || c == '#' || c == '+')
            {
                sb.Append(c);
            }
            else if (sb.Length > 0)
            {
                list.Add(sb.ToString());
                sb.Clear();
            }
        }
        if (sb.Length > 0) list.Add(sb.ToString());
        return list;
    }

    private static string FoldDiacritics(string input)
    {
        string replaced = input.Replace('đ', 'd').Replace('Đ', 'D');
        string normalized = replaced.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static uint Fnv1aHash(string input)
    {
        uint hash = 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(input))
        {
            hash ^= b;
            hash *= 16777619;
        }
        return hash;
    }
}
