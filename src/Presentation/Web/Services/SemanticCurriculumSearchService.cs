using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
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
    public List<UnifiedKnowledgeHit> Hits { get; set; } = [];
}

public interface ISemanticCurriculumSearchService
{
    Task<SemanticSearchResponse> SearchAsync(string query, bool isEnglish, int topK = 5, CancellationToken cancellationToken = default);
    Task<string> BuildRagContextAsync(string query, bool isEnglish, int topK = 3, CancellationToken cancellationToken = default);
    Task<UnifiedKnowledgeSearchResponse> SearchKnowledgeBaseAsync(string query, bool isEnglish, int topK = 5, CancellationToken cancellationToken = default);
    Task<string> BuildUnifiedRagContextAsync(string query, bool isEnglish, int topK = 4, CancellationToken cancellationToken = default);
    Task<int> GetIndexedKnowledgeCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Hybrid RAG vector knowledge base indexing both the 101-lesson C# curriculum AND all persisted
/// database entities (Tutorials Markdown bodies, CodingChallenges, and InterviewFlashcards).
/// Uses 4096-dimensional FNV-1a hashed TF-IDF sparse vectors, Vietnamese diacritic folding,
/// character tri-grams, word bi-grams, and L2 cosine similarity ranking.
/// </summary>
public sealed class SemanticCurriculumSearchService : ISemanticCurriculumSearchService
{
    public const int Dimensions = 4096;
    private static readonly TimeSpan UnifiedIndexTtl = TimeSpan.FromMinutes(15);

    private readonly ICurriculumService _curriculumService;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly AppDbContext? _directDbContext;
    private UnifiedVectorIndex? _localUnifiedIndex;

    private static readonly SemaphoreSlim _buildLock = new(1, 1);
    private static VectorIndex? _index;

    private static readonly SemaphoreSlim _unifiedBuildLock = new(1, 1);
    private static UnifiedVectorIndex? _unifiedIndex;
    private static DateTime _unifiedBuiltAtUtc = DateTime.MinValue;

    public SemanticCurriculumSearchService(
        ICurriculumService curriculumService,
        IServiceScopeFactory? scopeFactory = null,
        AppDbContext? directDbContext = null)
    {
        _curriculumService = curriculumService;
        _scopeFactory = scopeFactory;
        _directDbContext = directDbContext;
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
            SourceCounts = unified.SourceCounts
        };

        if (string.IsNullOrWhiteSpace(query))
        {
            sw.Stop();
            response.ElapsedMicroseconds = (long)(sw.Elapsed.TotalMilliseconds * 1000);
            return response;
        }

        var queryVector = VectorMath.Vectorize(Tokenize(query), unified.Idf);
        int k = Math.Clamp(topK, 1, 20);

        response.Hits = unified.Documents
            .Select(doc => (doc, score: Cosine(queryVector, doc.Vector)))
            .Where(x => x.score > 0.015)
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

            var built = UnifiedVectorIndex.Build(rawDocs);
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

            // Character tri-grams provide fuzzy matching for typos and inflections
            if (token.Length >= 4)
            {
                string padded = $"^{token}$";
                for (int i = 0; i + 3 <= padded.Length; i++)
                {
                    features.Add("g:" + padded.Substring(i, 3));
                }
            }
        }

        // Word bi-grams capture short phrases such as "dependency injection" or "async await"
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
        // FNV-1a 32-bit hash for stable, process-independent feature hashing
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
        return dot; // Vectors are L2-normalized at build time
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
        string SourceType,
        string TitleVi,
        string TitleEn,
        string SummaryVi,
        string SummaryEn,
        string UrlVi,
        string UrlEn,
        Dictionary<int, double> Vector);

    private sealed class UnifiedVectorIndex
    {
        public List<IndexedKnowledgeDoc> Documents { get; } = [];
        public Dictionary<string, int> SourceCounts { get; } = new();
        public double[] Idf { get; }

        private UnifiedVectorIndex(double[] idf) => Idf = idf;

        public static UnifiedVectorIndex Build(IReadOnlyList<UnifiedKnowledgeRawDoc> docs)
        {
            var tokenized = docs.Select(d => (doc: d, features: Tokenize(d.SearchableText))).ToList();
            var idf = VectorMath.ComputeIdf(tokenized.Select(t => t.features), tokenized.Count);
            var index = new UnifiedVectorIndex(idf);

            foreach (var (doc, features) in tokenized)
            {
                index.Documents.Add(new IndexedKnowledgeDoc(
                    doc.SourceType, doc.TitleVi, doc.TitleEn, doc.SummaryVi, doc.SummaryEn,
                    doc.UrlVi, doc.UrlEn, VectorMath.Vectorize(features, idf)));
                index.SourceCounts[doc.SourceType] = index.SourceCounts.GetValueOrDefault(doc.SourceType) + 1;
            }
            return index;
        }
    }
}
