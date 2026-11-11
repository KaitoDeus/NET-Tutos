using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public sealed class SemanticSearchHit
{
    public int LessonNumber { get; set; }
    public int SectionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public List<string> KeyTags { get; set; } = [];
    public double Score { get; set; }
}

public sealed class SemanticSearchResponse
{
    public string Query { get; set; } = string.Empty;
    public string Language { get; set; } = "vi";
    public int IndexedDocuments { get; set; }
    public int VectorDimensions { get; set; }
    public long ElapsedMicroseconds { get; set; }
    public List<SemanticSearchHit> Hits { get; set; } = [];
}

public sealed class UnifiedKnowledgeHit
{
    public string SourceType { get; set; } = "Curriculum";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public double Score { get; set; }
}

public sealed class UnifiedKnowledgeSearchResponse
{
    public string Query { get; set; } = string.Empty;
    public string Language { get; set; } = "vi";
    public int TotalIndexedDocuments { get; set; }
    public Dictionary<string, int> SourceCounts { get; set; } = new();
    public long ElapsedMicroseconds { get; set; }
    public string SearchMode { get; set; } = "dense-neural-hybrid";
    public string EmbeddingModel { get; set; } = "nettutos-neural-hash-768";
    public int NeuralDimensions { get; set; } = GeminiNeuralEmbeddingService.DefaultDimensions;
    public bool PgVectorEnabled { get; set; }
    public List<UnifiedKnowledgeHit> Hits { get; set; } = [];
}

public sealed class EmbeddingSyncResult
{
    public bool Success { get; set; }
    public int TotalDocuments { get; set; }
    public int UpdatedEmbeddings { get; set; }
    public int ReusedCachedEmbeddings { get; set; }
    public int NeuralDimensions { get; set; } = GeminiNeuralEmbeddingService.DefaultDimensions;
    public string EmbeddingModel { get; set; } = "nettutos-neural-hash-768";
    public bool PgVectorActive { get; set; }
    public long ElapsedMs { get; set; }
}

public sealed class EmbeddingStatusResponse
{
    public int PersistedEmbeddingsCount { get; set; }
    public int InMemoryDocumentsCount { get; set; }
    public string EmbeddingModel { get; set; } = "nettutos-neural-hash-768";
    public int NeuralDimensions { get; set; } = GeminiNeuralEmbeddingService.DefaultDimensions;
    public bool PgVectorExtensionActive { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }
}

public interface ISemanticCurriculumSearchService
{
    Task<SemanticSearchResponse> SearchAsync(string query, bool isEnglish, int topK = 5, CancellationToken cancellationToken = default);
    Task<string> BuildRagContextAsync(string query, bool isEnglish, int topK = 3, CancellationToken cancellationToken = default);
    Task<UnifiedKnowledgeSearchResponse> SearchKnowledgeBaseAsync(string query, bool isEnglish, int topK = 5, CancellationToken cancellationToken = default);
    Task<string> BuildUnifiedRagContextAsync(string query, bool isEnglish, int topK = 4, CancellationToken cancellationToken = default);
    Task<int> GetIndexedKnowledgeCountAsync(CancellationToken cancellationToken = default);
    Task<EmbeddingSyncResult> SyncKnowledgeEmbeddingsAsync(bool forceRebuild = false, CancellationToken cancellationToken = default);
    Task<EmbeddingStatusResponse> GetEmbeddingStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Hybrid Neural + Lexical RAG vector knowledge base indexing both the 101-lesson C# curriculum
/// AND persisted database entities (Tutorials Markdown, CodingChallenges, and InterviewFlashcards).
/// Combines 768-dim Neural Embeddings (Gemini text-embedding-004 + PostgreSQL pgvector HNSW when online,
/// or multilingual EN/VI dense fallback offline) with 4096-dim sparse TF-IDF cosine ranking.
/// </summary>
public sealed class SemanticCurriculumSearchService : ISemanticCurriculumSearchService
{
    public const int Dimensions = 4096;
    private static readonly TimeSpan UnifiedIndexTtl = TimeSpan.FromMinutes(15);

    private readonly ICurriculumService _curriculumService;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly AppDbContext? _directDbContext;
    private readonly INeuralEmbeddingService _embeddingService;
    private UnifiedVectorIndex? _localUnifiedIndex;

    private static readonly SemaphoreSlim _buildLock = new(1, 1);
    private static VectorIndex? _index;

    private static readonly SemaphoreSlim _unifiedBuildLock = new(1, 1);
    private static UnifiedVectorIndex? _unifiedIndex;
    private static DateTime _unifiedBuiltAtUtc = DateTime.MinValue;

    public SemanticCurriculumSearchService(
        ICurriculumService curriculumService,
        IServiceScopeFactory? scopeFactory = null,
        AppDbContext? directDbContext = null,
        INeuralEmbeddingService? embeddingService = null)
    {
        _curriculumService = curriculumService;
        _scopeFactory = scopeFactory;
        _directDbContext = directDbContext;
        _embeddingService = embeddingService ?? new GeminiNeuralEmbeddingService();
    }

    public async Task<SemanticSearchResponse> SearchAsync(string query, bool isEnglish, int topK = 5, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var index = await EnsureIndexAsync(cancellationToken);
        var response = new SemanticSearchResponse
        {
            Query = query ?? string.Empty,
            Language = isEnglish ? "en" : "vi",
            IndexedDocuments = index.Documents.Count,
            VectorDimensions = Dimensions
        };

        if (string.IsNullOrWhiteSpace(query))
        {
            sw.Stop();
            response.ElapsedMicroseconds = (long)(sw.Elapsed.TotalMilliseconds * 1000);
            return response;
        }

        var queryVector = index.Vectorize(Tokenize(query));
        int k = Math.Clamp(topK, 1, 20);

        response.Hits = index.Documents
            .Select(doc => (doc, score: Cosine(queryVector, doc.Vector)))
            .Where(x => x.score > 0.01)
            .OrderByDescending(x => x.score)
            .Take(k)
            .Select(x => new SemanticSearchHit
            {
                LessonNumber = x.doc.Lesson.LessonNumber,
                SectionNumber = x.doc.Lesson.SectionNumber,
                Title = isEnglish ? x.doc.Lesson.TitleEn : x.doc.Lesson.TitleVi,
                Summary = isEnglish ? x.doc.Lesson.SummaryEn : x.doc.Lesson.SummaryVi,
                Slug = x.doc.Lesson.Slug,
                KeyTags = x.doc.Lesson.KeyTags.ToList(),
                Score = Math.Round(x.score, 4)
            })
            .ToList();

        sw.Stop();
        response.ElapsedMicroseconds = (long)(sw.Elapsed.TotalMilliseconds * 1000);
        return response;
    }

    public async Task<string> BuildRagContextAsync(string query, bool isEnglish, int topK = 3, CancellationToken cancellationToken = default)
    {
        var result = await SearchAsync(query, isEnglish, topK, cancellationToken);
        if (result.Hits.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(isEnglish ? "Relevant NET-Tutos curriculum lessons:" : "Các bài học liên quan trong giáo trình NET-Tutos:");
        foreach (var hit in result.Hits)
        {
            sb.AppendLine($"- Lesson {hit.LessonNumber} (Part {hit.SectionNumber}): {hit.Title} — {hit.Summary} [/csharp-curriculum/{hit.LessonNumber}]");
        }
        return sb.ToString();
    }

    public async Task<UnifiedKnowledgeSearchResponse> SearchKnowledgeBaseAsync(string query, bool isEnglish, int topK = 5, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var unified = await EnsureUnifiedIndexAsync(cancellationToken);

        var response = new UnifiedKnowledgeSearchResponse
        {
            Query = query ?? string.Empty,
            Language = isEnglish ? "en" : "vi",
            TotalIndexedDocuments = unified.Documents.Count,
            SourceCounts = unified.SourceCounts,
            EmbeddingModel = _embeddingService.ActiveModelName,
            NeuralDimensions = _embeddingService.NeuralDimensions
        };

        if (string.IsNullOrWhiteSpace(query))
        {
            sw.Stop();
            response.ElapsedMicroseconds = (long)(sw.Elapsed.TotalMilliseconds * 1000);
            return response;
        }

        var queryLexical = VectorMath.Vectorize(Tokenize(query), unified.Idf);
        var queryNeural = await _embeddingService.EmbedAsync(query, cancellationToken);
        int k = Math.Clamp(topK, 1, 20);

        // Try native PostgreSQL pgvector cosine <=> search when running on pgvector-enabled Postgres
        var pgVectorScores = await TryQueryPgVectorSimilaritiesAsync(queryNeural.Vector, Math.Max(k * 4, 24), cancellationToken);
        if (pgVectorScores.Count > 0)
        {
            response.PgVectorEnabled = true;
            response.SearchMode = "pgvector-neural-hybrid";
        }
        else
        {
            response.PgVectorEnabled = false;
            response.SearchMode = "dense-neural-hybrid";
        }

        response.Hits = unified.Documents
            .Select(doc =>
            {
                double lexical = Cosine(queryLexical, doc.Vector);
                double neural = pgVectorScores.TryGetValue(doc.DocumentKey, out var pgSim)
                    ? Math.Max(0, pgSim)
                    : Math.Max(0, GeminiNeuralEmbeddingService.CosineSimilarity(queryNeural.Vector, doc.NeuralVector));

                double hybridScore = (0.55 * neural) + (0.45 * lexical);
                return (doc, score: hybridScore);
            })
            .Where(x => x.score > 0.025)
            .OrderByDescending(x => x.score)
            .Take(k)
            .Select(x => new UnifiedKnowledgeHit
            {
                SourceType = x.doc.SourceType,
                Title = isEnglish ? x.doc.TitleEn : x.doc.TitleVi,
                Summary = isEnglish ? x.doc.SummaryEn : x.doc.SummaryVi,
                Url = isEnglish ? x.doc.UrlEn : x.doc.UrlVi,
                Score = Math.Round(x.score, 4)
            })
            .ToList();

        sw.Stop();
        response.ElapsedMicroseconds = (long)(sw.Elapsed.TotalMilliseconds * 1000);
        return response;
    }

    public async Task<string> BuildUnifiedRagContextAsync(string query, bool isEnglish, int topK = 4, CancellationToken cancellationToken = default)
    {
        var result = await SearchKnowledgeBaseAsync(query, isEnglish, topK, cancellationToken);
        if (result.Hits.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(isEnglish
            ? "\n[NET-Tutos Multi-Source Knowledge RAG Context (Cite exact Markdown links [Title](Url) if relevant)]:"
            : "\n[Ngữ cảnh Tri thức Đa nguồn NET-Tutos RAG (Hãy trích dẫn link Markdown chính xác [Tên tài liệu](Url) cho học viên nếu phù hợp)]:");

        foreach (var hit in result.Hits)
        {
            sb.AppendLine($"- [{hit.SourceType}] [{hit.Title}]({hit.Url}): {hit.Summary}");
        }

        return sb.ToString();
    }

    public async Task<int> GetIndexedKnowledgeCountAsync(CancellationToken cancellationToken = default)
    {
        var unified = await EnsureUnifiedIndexAsync(cancellationToken);
        return unified.Documents.Count;
    }

    public async Task<EmbeddingSyncResult> SyncKnowledgeEmbeddingsAsync(bool forceRebuild = false, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var rawDocs = await CollectUnifiedRawDocsAsync(cancellationToken);
        var modelName = _embeddingService.ActiveModelName;

        int updated = 0;
        int reused = 0;
        bool pgVectorActive = false;

        AppDbContext? db = _directDbContext;
        IServiceScope? scope = null;
        if (db == null && _scopeFactory != null)
        {
            scope = _scopeFactory.CreateScope();
            db = scope.ServiceProvider.GetService<AppDbContext>();
        }

        try
        {
            if (db != null)
            {
                var existingByKey = await db.KnowledgeEmbeddings
                    .ToDictionaryAsync(e => e.DocumentKey, StringComparer.OrdinalIgnoreCase, cancellationToken);

                bool isNpgsql = IsNpgsqlDatabase(db);
                if (isNpgsql)
                {
                    pgVectorActive = await CheckPgVectorAvailableAsync(db, cancellationToken);
                }

                foreach (var doc in rawDocs)
                {
                    string docKey = BuildDocumentKey(doc);
                    string contentHash = GeminiNeuralEmbeddingService.ComputeSha256($"{doc.TitleVi}|{doc.TitleEn}|{doc.SearchableText}");

                    if (!forceRebuild
                        && existingByKey.TryGetValue(docKey, out var existing)
                        && string.Equals(existing.ContentHash, contentHash, StringComparison.Ordinal)
                        && string.Equals(existing.EmbeddingModel, modelName, StringComparison.OrdinalIgnoreCase))
                    {
                        reused++;
                        continue;
                    }

                    var embResult = await _embeddingService.EmbedAsync(doc.SearchableText, cancellationToken);
                    string vectorJson = JsonSerializer.Serialize(embResult.Vector);

                    if (existingByKey.TryGetValue(docKey, out var row))
                    {
                        row.SourceType = doc.SourceType;
                        row.TitleVi = doc.TitleVi;
                        row.TitleEn = doc.TitleEn;
                        row.SummaryVi = doc.SummaryVi;
                        row.SummaryEn = doc.SummaryEn;
                        row.UrlVi = doc.UrlVi;
                        row.UrlEn = doc.UrlEn;
                        row.ContentHash = contentHash;
                        row.EmbeddingModel = embResult.ModelName;
                        row.Dimensions = embResult.Vector.Length;
                        row.VectorJson = vectorJson;
                        row.UpdatedAtUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        row = new KnowledgeEmbedding
                        {
                            DocumentKey = docKey,
                            SourceType = doc.SourceType,
                            TitleVi = doc.TitleVi,
                            TitleEn = doc.TitleEn,
                            SummaryVi = doc.SummaryVi,
                            SummaryEn = doc.SummaryEn,
                            UrlVi = doc.UrlVi,
                            UrlEn = doc.UrlEn,
                            ContentHash = contentHash,
                            EmbeddingModel = embResult.ModelName,
                            Dimensions = embResult.Vector.Length,
                            VectorJson = vectorJson,
                            UpdatedAtUtc = DateTime.UtcNow
                        };
                        db.KnowledgeEmbeddings.Add(row);
                    }

                    updated++;
                }

                await db.SaveChangesAsync(cancellationToken);

                if (pgVectorActive)
                {
                    var rowsToVectorize = await db.KnowledgeEmbeddings
                        .AsNoTracking()
                        .Select(e => new { e.DocumentKey, e.VectorJson })
                        .ToListAsync(cancellationToken);

                    foreach (var r in rowsToVectorize)
                    {
                        var parsed = DeserializeVector(r.VectorJson);
                        if (parsed != null)
                        {
                            string pgLit = GeminiNeuralEmbeddingService.ToPgVectorLiteral(parsed);
                            await db.Database.ExecuteSqlInterpolatedAsync(
                                $@"UPDATE ""KnowledgeEmbeddings"" SET ""EmbeddingVector"" = CAST({pgLit} AS vector) WHERE ""DocumentKey"" = {r.DocumentKey}",
                                cancellationToken);
                        }
                    }
                }
            }
        }
        finally
        {
            scope?.Dispose();
        }

        // Refresh in-memory index after sync
        _unifiedBuiltAtUtc = DateTime.MinValue;
        await EnsureUnifiedIndexAsync(cancellationToken);

        sw.Stop();
        return new EmbeddingSyncResult
        {
            Success = true,
            TotalDocuments = rawDocs.Count,
            UpdatedEmbeddings = updated,
            ReusedCachedEmbeddings = reused,
            NeuralDimensions = _embeddingService.NeuralDimensions,
            EmbeddingModel = modelName,
            PgVectorActive = pgVectorActive,
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }

    public async Task<EmbeddingStatusResponse> GetEmbeddingStatusAsync(CancellationToken cancellationToken = default)
    {
        int inMemoryCount = await GetIndexedKnowledgeCountAsync(cancellationToken);
        int persistedCount = 0;
        bool pgVectorActive = false;
        DateTime? lastSynced = null;

        AppDbContext? db = _directDbContext;
        IServiceScope? scope = null;
        if (db == null && _scopeFactory != null)
        {
            scope = _scopeFactory.CreateScope();
            db = scope.ServiceProvider.GetService<AppDbContext>();
        }

        try
        {
            if (db != null)
            {
                persistedCount = await db.KnowledgeEmbeddings.CountAsync(cancellationToken);
                if (persistedCount > 0)
                {
                    lastSynced = await db.KnowledgeEmbeddings.MaxAsync(e => (DateTime?)e.UpdatedAtUtc, cancellationToken);
                }
                if (IsNpgsqlDatabase(db))
                {
                    pgVectorActive = await CheckPgVectorAvailableAsync(db, cancellationToken);
                }
            }
        }
        catch
        {
            // Ignore when optional embedding table does not exist in lightweight unit test DB
        }
        finally
        {
            scope?.Dispose();
        }

        return new EmbeddingStatusResponse
        {
            PersistedEmbeddingsCount = persistedCount,
            InMemoryDocumentsCount = inMemoryCount,
            EmbeddingModel = _embeddingService.ActiveModelName,
            NeuralDimensions = _embeddingService.NeuralDimensions,
            PgVectorExtensionActive = pgVectorActive,
            LastSyncedAtUtc = lastSynced
        };
    }

    private async Task<VectorIndex> EnsureIndexAsync(CancellationToken cancellationToken)
    {
        if (_index != null) return _index;

        await _buildLock.WaitAsync(cancellationToken);
        try
        {
            if (_index != null) return _index;
            var lessons = await _curriculumService.GetAllLessonsAsync();
            _index = VectorIndex.Build(lessons);
            return _index;
        }
        finally
        {
            _buildLock.Release();
        }
    }

    private async Task<UnifiedVectorIndex> EnsureUnifiedIndexAsync(CancellationToken cancellationToken)
    {
        if (_directDbContext != null && _localUnifiedIndex != null)
        {
            return _localUnifiedIndex;
        }

        if (_directDbContext == null && _unifiedIndex != null && (DateTime.UtcNow - _unifiedBuiltAtUtc) < UnifiedIndexTtl)
        {
            return _unifiedIndex;
        }

        await _unifiedBuildLock.WaitAsync(cancellationToken);
        try
        {
            if (_directDbContext != null && _localUnifiedIndex != null)
            {
                return _localUnifiedIndex;
            }

            if (_directDbContext == null && _unifiedIndex != null && (DateTime.UtcNow - _unifiedBuiltAtUtc) < UnifiedIndexTtl)
            {
                return _unifiedIndex;
            }

            var rawDocs = await CollectUnifiedRawDocsAsync(cancellationToken);
            var persistedVectors = await LoadCachedVectorsFromDbAsync(cancellationToken);
            var built = UnifiedVectorIndex.Build(rawDocs, persistedVectors);

            if (_directDbContext != null)
            {
                _localUnifiedIndex = built;
            }
            else
            {
                _unifiedIndex = built;
                _unifiedBuiltAtUtc = DateTime.UtcNow;
            }
            return built;
        }
        finally
        {
            _unifiedBuildLock.Release();
        }
    }

    private async Task<List<UnifiedKnowledgeRawDoc>> CollectUnifiedRawDocsAsync(CancellationToken cancellationToken)
    {
        var rawDocs = new List<UnifiedKnowledgeRawDoc>();

        // 1. Curriculum 101 lessons
        var lessons = await _curriculumService.GetAllLessonsAsync();
        foreach (var l in lessons)
        {
            rawDocs.Add(new UnifiedKnowledgeRawDoc(
                SourceType: "Curriculum",
                TitleVi: l.TitleVi,
                TitleEn: l.TitleEn,
                SummaryVi: l.SummaryVi,
                SummaryEn: l.SummaryEn,
                UrlVi: $"/chuong-trinh-csharp/{l.LessonNumber}",
                UrlEn: $"/csharp-curriculum/{l.LessonNumber}",
                SearchableText: string.Join(' ',
                    l.TitleVi, l.TitleVi, l.TitleEn, l.TitleEn,
                    string.Join(' ', l.KeyTags), string.Join(' ', l.KeyTags),
                    l.SummaryVi, l.SummaryEn,
                    string.Join(' ', l.LearningOutcomesVi), string.Join(' ', l.LearningOutcomesEn))
            ));
        }

        // 2. Persistable DB Entities (Tutorials, CodingChallenges, InterviewFlashcards)
        AppDbContext? db = _directDbContext;
        IServiceScope? scope = null;
        if (db == null && _scopeFactory != null)
        {
            scope = _scopeFactory.CreateScope();
            db = scope.ServiceProvider.GetService<AppDbContext>();
        }

        try
        {
            if (db != null)
            {
                var tutorials = await db.Tutorials
                    .AsNoTracking()
                    .OrderBy(t => t.OrderIndex)
                    .Select(t => new { t.Title, t.Slug, t.Summary, t.ContentMarkdown })
                    .ToListAsync(cancellationToken);

                foreach (var t in tutorials)
                {
                    var bodyExcerpt = TruncateText(t.ContentMarkdown, 900);
                    rawDocs.Add(new UnifiedKnowledgeRawDoc(
                        SourceType: "Tutorial",
                        TitleVi: t.Title,
                        TitleEn: t.Title,
                        SummaryVi: t.Summary,
                        SummaryEn: t.Summary,
                        UrlVi: $"/lessons/{t.Slug}",
                        UrlEn: $"/lessons/{t.Slug}",
                        SearchableText: $"{t.Title} {t.Title} {t.Summary} {bodyExcerpt}"
                    ));
                }

                var challenges = await db.CodingChallenges
                    .AsNoTracking()
                    .Select(c => new { c.Title, c.Slug, c.ShortDescription, c.InstructionsMarkdown })
                    .ToListAsync(cancellationToken);

                foreach (var c in challenges)
                {
                    var excerpt = TruncateText(c.InstructionsMarkdown, 600);
                    rawDocs.Add(new UnifiedKnowledgeRawDoc(
                        SourceType: "Challenge",
                        TitleVi: c.Title,
                        TitleEn: c.Title,
                        SummaryVi: c.ShortDescription,
                        SummaryEn: c.ShortDescription,
                        UrlVi: $"/Playground?slug={c.Slug}",
                        UrlEn: $"/Playground?slug={c.Slug}",
                        SearchableText: $"{c.Title} {c.Title} {c.ShortDescription} {excerpt}"
                    ));
                }

                var flashcards = await db.InterviewFlashcards
                    .AsNoTracking()
                    .Select(f => new { f.Slug, f.Topic, f.QuestionVi, f.QuestionEn, f.QuickHintVi, f.QuickHintEn, f.AnswerMarkdownVi, f.AnswerMarkdownEn })
                    .ToListAsync(cancellationToken);

                foreach (var f in flashcards)
                {
                    rawDocs.Add(new UnifiedKnowledgeRawDoc(
                        SourceType: "Flashcard",
                        TitleVi: f.QuestionVi,
                        TitleEn: f.QuestionEn,
                        SummaryVi: f.QuickHintVi ?? TruncateText(f.AnswerMarkdownVi, 180),
                        SummaryEn: f.QuickHintEn ?? TruncateText(f.AnswerMarkdownEn, 180),
                        UrlVi: $"/Interview/QuestionDetail?slug={f.Slug}",
                        UrlEn: $"/Interview/QuestionDetail?slug={f.Slug}",
                        SearchableText: $"{f.Topic} {f.QuestionVi} {f.QuestionEn} {TruncateText(f.AnswerMarkdownVi, 450)} {TruncateText(f.AnswerMarkdownEn, 450)}"
                    ));
                }
            }
        }
        catch
        {
            // Keep curriculum index intact even if optional DB tables aren't initialized
        }
        finally
        {
            scope?.Dispose();
        }

        return rawDocs;
    }

    private async Task<Dictionary<string, float[]>> LoadCachedVectorsFromDbAsync(CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);

        AppDbContext? db = _directDbContext;
        IServiceScope? scope = null;
        if (db == null && _scopeFactory != null)
        {
            scope = _scopeFactory.CreateScope();
            db = scope.ServiceProvider.GetService<AppDbContext>();
        }

        try
        {
            if (db != null)
            {
                var rows = await db.KnowledgeEmbeddings
                    .AsNoTracking()
                    .Select(e => new { e.DocumentKey, e.VectorJson })
                    .ToListAsync(cancellationToken);

                foreach (var r in rows)
                {
                    var vec = DeserializeVector(r.VectorJson);
                    if (vec != null) map[r.DocumentKey] = vec;
                }
            }
        }
        catch
        {
            // Optional table during early startup or bare unit tests
        }
        finally
        {
            scope?.Dispose();
        }

        return map;
    }

    private async Task<Dictionary<string, double>> TryQueryPgVectorSimilaritiesAsync(float[] queryVector, int limit, CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (_scopeFactory == null && _directDbContext == null) return map;

        AppDbContext? db = _directDbContext;
        IServiceScope? scope = null;
        if (db == null && _scopeFactory != null)
        {
            scope = _scopeFactory.CreateScope();
            db = scope.ServiceProvider.GetService<AppDbContext>();
        }

        try
        {
            if (db == null || !IsNpgsqlDatabase(db)) return map;

            var conn = db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ""DocumentKey"", 1.0 - (""EmbeddingVector"" <=> CAST(@qvec AS vector)) AS similarity
                FROM ""KnowledgeEmbeddings""
                WHERE ""EmbeddingVector"" IS NOT NULL
                ORDER BY ""EmbeddingVector"" <=> CAST(@qvec AS vector)
                LIMIT @lim;";

            var pVec = cmd.CreateParameter();
            pVec.ParameterName = "@qvec";
            pVec.Value = GeminiNeuralEmbeddingService.ToPgVectorLiteral(queryVector);
            cmd.Parameters.Add(pVec);

            var pLim = cmd.CreateParameter();
            pLim.ParameterName = "@lim";
            pLim.Value = limit;
            cmd.Parameters.Add(pLim);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                string key = reader.GetString(0);
                double sim = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                map[key] = sim;
            }
        }
        catch
        {
            // Fall back smoothly when pgvector extension isn't active or SQLite is running
        }
        finally
        {
            scope?.Dispose();
        }

        return map;
    }

    private static bool IsNpgsqlDatabase(AppDbContext db)
    {
        string? provider = db.Database.ProviderName;
        return !string.IsNullOrEmpty(provider) && provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> CheckPgVectorAvailableAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM pg_extension WHERE extname = 'vector';";
            var val = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(val, CultureInfo.InvariantCulture) > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildDocumentKey(UnifiedKnowledgeRawDoc doc)
    {
        string rawKey = $"{doc.SourceType}:{doc.UrlVi}";
        return rawKey.Length <= 160 ? rawKey : rawKey[..160];
    }

    private static float[]? DeserializeVector(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var arr = JsonSerializer.Deserialize<float[]>(json);
            return arr != null && arr.Length > 0 ? arr : null;
        }
        catch
        {
            return null;
        }
    }

    private static string TruncateText(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Length <= maxLen ? value : value[..maxLen];
    }

    internal static List<string> Tokenize(string text)
    {
        var folded = FoldDiacritics(text.ToLowerInvariant());
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (char c in folded)
        {
            if (char.IsLetterOrDigit(c) || c == '#' || c == '+')
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                tokens.Add(current.ToString());
                current.Clear();
            }
        }
        if (current.Length > 0) tokens.Add(current.ToString());

        var features = new List<string>(tokens.Count * 3);
        foreach (var token in tokens)
        {
            if (token.Length < 2 || StopWords.Contains(token)) continue;
            features.Add("w:" + token);

            if (token.Length >= 4)
            {
                string padded = $"^{token}$";
                for (int i = 0; i + 3 <= padded.Length; i++)
                {
                    features.Add("g:" + padded.Substring(i, 3));
                }
            }
        }

        for (int i = 0; i + 1 < tokens.Count; i++)
        {
            features.Add($"b:{tokens[i]}_{tokens[i + 1]}");
        }

        return features;
    }

    private static string FoldDiacritics(string input)
    {
        var normalized = input.Replace('đ', 'd').Normalize(NormalizationForm.FormD);
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

    internal static int HashFeature(string feature)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in feature)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return (int)(hash % Dimensions);
        }
    }

    private static double Cosine(Dictionary<int, double> a, Dictionary<int, double> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
        double dot = 0;
        foreach (var (key, value) in small)
        {
            if (large.TryGetValue(key, out var other)) dot += value * other;
        }
        return dot;
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the", "and", "for", "with", "in", "of", "to", "is", "an", "on", "how", "what", "use", "using",
        "va", "cua", "cac", "trong", "voi", "la", "cho", "nhu", "the", "nao", "gi", "mot", "nhung", "duoc", "khi"
    };

    private static class VectorMath
    {
        public static double[] ComputeIdf(IEnumerable<List<string>> docFeaturesList, int docCount)
        {
            var docFreq = new int[Dimensions];
            foreach (var features in docFeaturesList)
            {
                foreach (var bucket in features.Select(HashFeature).Distinct())
                {
                    docFreq[bucket]++;
                }
            }

            int n = Math.Max(1, docCount);
            var idf = new double[Dimensions];
            for (int i = 0; i < Dimensions; i++)
            {
                idf[i] = Math.Log(1.0 + (double)n / (1 + docFreq[i]));
            }
            return idf;
        }

        public static Dictionary<int, double> Vectorize(List<string> features, double[] idf)
        {
            var tf = new Dictionary<int, double>();
            foreach (var f in features)
            {
                int bucket = HashFeature(f);
                double weight = f.StartsWith("g:", StringComparison.Ordinal) ? 0.35 : f.StartsWith("b:", StringComparison.Ordinal) ? 1.5 : 1.0;
                tf[bucket] = tf.GetValueOrDefault(bucket) + weight;
            }

            double norm = 0;
            foreach (var key in tf.Keys.ToList())
            {
                double v = (1 + Math.Log(tf[key])) * idf[key];
                tf[key] = v;
                norm += v * v;
            }

            norm = Math.Sqrt(norm);
            if (norm > 0)
            {
                foreach (var key in tf.Keys.ToList()) tf[key] /= norm;
            }
            return tf;
        }
    }

    private sealed record IndexedLesson(CurriculumLessonViewModel Lesson, Dictionary<int, double> Vector);

    private sealed class VectorIndex
    {
        public List<IndexedLesson> Documents { get; } = [];
        private readonly double[] _idf;

        private VectorIndex(double[] idf) => _idf = idf;

        public static VectorIndex Build(IReadOnlyList<CurriculumLessonViewModel> lessons)
        {
            var docFeatures = lessons.Select(l => (lesson: l, features: Tokenize(BuildDocumentText(l)))).ToList();
            var idf = VectorMath.ComputeIdf(docFeatures.Select(d => d.features), docFeatures.Count);
            var index = new VectorIndex(idf);

            foreach (var (lesson, features) in docFeatures)
            {
                index.Documents.Add(new IndexedLesson(lesson, VectorMath.Vectorize(features, idf)));
            }
            return index;
        }

        public Dictionary<int, double> Vectorize(List<string> features) => VectorMath.Vectorize(features, _idf);

        private static string BuildDocumentText(CurriculumLessonViewModel l)
        {
            var tags = string.Join(' ', l.KeyTags);
            return string.Join(' ',
                l.TitleVi, l.TitleVi, l.TitleEn, l.TitleEn,
                tags, tags,
                l.SummaryVi, l.SummaryEn,
                string.Join(' ', l.LearningOutcomesVi), string.Join(' ', l.LearningOutcomesEn));
        }
    }

    private sealed record UnifiedKnowledgeRawDoc(
        string SourceType,
        string TitleVi,
        string TitleEn,
        string SummaryVi,
        string SummaryEn,
        string UrlVi,
        string UrlEn,
        string SearchableText);

    private sealed record IndexedKnowledgeDoc(
        string DocumentKey,
        string SourceType,
        string TitleVi,
        string TitleEn,
        string SummaryVi,
        string SummaryEn,
        string UrlVi,
        string UrlEn,
        Dictionary<int, double> Vector,
        float[] NeuralVector);

    private sealed class UnifiedVectorIndex
    {
        public List<IndexedKnowledgeDoc> Documents { get; } = [];
        public Dictionary<string, int> SourceCounts { get; } = new();
        public double[] Idf { get; }

        private UnifiedVectorIndex(double[] idf) => Idf = idf;

        public static UnifiedVectorIndex Build(
            IReadOnlyList<UnifiedKnowledgeRawDoc> docs,
            Dictionary<string, float[]> persistedVectors)
        {
            var tokenized = docs.Select(d => (doc: d, features: Tokenize(d.SearchableText))).ToList();
            var idf = VectorMath.ComputeIdf(tokenized.Select(t => t.features), tokenized.Count);
            var index = new UnifiedVectorIndex(idf);

            foreach (var (doc, features) in tokenized)
            {
                string docKey = BuildDocumentKey(doc);
                float[] neuralVec = persistedVectors.TryGetValue(docKey, out var cached)
                    ? cached
                    : GeminiNeuralEmbeddingService.ComputeMultilingualDenseProjection(doc.SearchableText);

                index.Documents.Add(new IndexedKnowledgeDoc(
                    docKey,
                    doc.SourceType,
                    doc.TitleVi,
                    doc.TitleEn,
                    doc.SummaryVi,
                    doc.SummaryEn,
                    doc.UrlVi,
                    doc.UrlEn,
                    VectorMath.Vectorize(features, idf),
                    neuralVec));
                index.SourceCounts[doc.SourceType] = index.SourceCounts.GetValueOrDefault(doc.SourceType) + 1;
            }
            return index;
        }
    }
}
