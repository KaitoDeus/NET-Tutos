using System.Globalization;
using System.Text;
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

public interface ISemanticCurriculumSearchService
{
    Task<SemanticSearchResponse> SearchAsync(string query, bool isEnglish, int topK = 5, CancellationToken cancellationToken = default);
    Task<string> BuildRagContextAsync(string query, bool isEnglish, int topK = 3, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-process RAG vector knowledge base over the bilingual curriculum.
/// Uses hashed TF-IDF sparse vectors (feature hashing, 4096 dimensions) with diacritic folding,
/// character tri-grams for fuzzy matching, and cosine similarity ranking. No external embedding API is required.
/// </summary>
public sealed class SemanticCurriculumSearchService : ISemanticCurriculumSearchService
{
    public const int Dimensions = 4096;

    private readonly ICurriculumService _curriculumService;
    private static readonly SemaphoreSlim _buildLock = new(1, 1);
    private static VectorIndex? _index;

    public SemanticCurriculumSearchService(ICurriculumService curriculumService)
    {
        _curriculumService = curriculumService;
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

    private static int HashFeature(string feature)
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

    private sealed record IndexedLesson(CurriculumLessonViewModel Lesson, Dictionary<int, double> Vector);

    private sealed class VectorIndex
    {
        public List<IndexedLesson> Documents { get; } = [];
        private readonly double[] _idf = new double[Dimensions];

        public static VectorIndex Build(IReadOnlyList<CurriculumLessonViewModel> lessons)
        {
            var index = new VectorIndex();
            var docFeatures = lessons.Select(l => (lesson: l, features: Tokenize(BuildDocumentText(l)))).ToList();

            var docFreq = new int[Dimensions];
            foreach (var (_, features) in docFeatures)
            {
                foreach (var bucket in features.Select(HashFeature).Distinct())
                {
                    docFreq[bucket]++;
                }
            }

            int n = Math.Max(1, docFeatures.Count);
            for (int i = 0; i < Dimensions; i++)
            {
                index._idf[i] = Math.Log(1.0 + (double)n / (1 + docFreq[i]));
            }

            foreach (var (lesson, features) in docFeatures)
            {
                index.Documents.Add(new IndexedLesson(lesson, index.Vectorize(features)));
            }
            return index;
        }

        public Dictionary<int, double> Vectorize(List<string> features)
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
                double v = (1 + Math.Log(tf[key])) * _idf[key];
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

        private static string BuildDocumentText(CurriculumLessonViewModel l)
        {
            // Titles and tags are repeated to boost their weight relative to body text
            var tags = string.Join(' ', l.KeyTags);
            return string.Join(' ',
                l.TitleVi, l.TitleVi, l.TitleEn, l.TitleEn,
                tags, tags,
                l.SummaryVi, l.SummaryEn,
                string.Join(' ', l.LearningOutcomesVi), string.Join(' ', l.LearningOutcomesEn));
        }
    }
}
