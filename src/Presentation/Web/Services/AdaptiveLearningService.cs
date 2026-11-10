using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;

namespace NET_Tutos.Services;

public sealed record AdaptiveRecommendationItem(
    string Title,
    string Url,
    string Reason,
    string Category,
    double RelevanceScore,
    string? RecommendedDifficulty = null
);

public sealed record AdaptiveLearningProfileResponse(
    bool IsAuthenticated,
    List<AdaptiveWeakSpotDto> DetectedWeakSpots,
    List<AdaptiveRecommendationItem> RecommendedLessons,
    List<string> TrendingQuestions,
    string SuggestedInterviewLevel,
    DateTime GeneratedAt
);

public interface IAdaptiveLearningService
{
    Task<AdaptiveLearningProfileResponse> GetRecommendationsAsync(string? userId, bool isEnglish, CancellationToken ct = default);
}

/// <summary>
/// Combines real-time Apache Kafka telemetry weak spots, historical Quiz/Interview gaps,
/// and the semantic RAG curriculum vector index to personalize the next learning steps for each student.
/// </summary>
public sealed class AdaptiveLearningService : IAdaptiveLearningService
{
    private readonly AppDbContext _context;
    private readonly ISemanticCurriculumSearchService _semanticSearch;
    private readonly IKafkaStreamAnalyticsStore _analyticsStore;

    public AdaptiveLearningService(
        AppDbContext context,
        ISemanticCurriculumSearchService semanticSearch,
        IKafkaStreamAnalyticsStore analyticsStore)
    {
        _context = context;
        _semanticSearch = semanticSearch;
        _analyticsStore = analyticsStore;
    }

    public async Task<AdaptiveLearningProfileResponse> GetRecommendationsAsync(string? userId, bool isEnglish, CancellationToken ct = default)
    {
        bool isAuth = !string.IsNullOrWhiteSpace(userId);
        var weakSpots = new List<AdaptiveWeakSpotDto>(_analyticsStore.GetUserWeakSpots(userId));

        // Enrich with persisted historical quiz gaps when authenticated
        if (isAuth)
        {
            try
            {
                var lowQuizzes = await _context.QuizAttempts
                    .AsNoTracking()
                    .Where(q => q.UserId == userId && (!q.IsPassed || q.ScorePercentage < 75))
                    .OrderByDescending(q => q.CompletedAt)
                    .Take(5)
                    .Select(q => new { q.ExamTitle, q.ScorePercentage })
                    .ToListAsync(ct);

                foreach (var quiz in lowQuizzes)
                {
                    if (!weakSpots.Any(w => w.Topic.Equals(quiz.ExamTitle, StringComparison.OrdinalIgnoreCase)))
                    {
                        weakSpots.Add(new AdaptiveWeakSpotDto(
                            Topic: quiz.ExamTitle,
                            HitCount: 1,
                            LowestScore: (int)Math.Round(quiz.ScorePercentage),
                            Source: "QuizAttempt"
                        ));
                    }
                }
            }
            catch
            {
                // Keep adaptive response resilient even if DB query fails
            }
        }

        var snapshot = _analyticsStore.GetSnapshot();
        var recommendedLessons = new List<AdaptiveRecommendationItem>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Query queries derived from weak spots first, then trending/foundational topics
        var searchSeeds = weakSpots.Select(w => (Query: w.Topic, IsWeakSpot: true, ReasonPrefix: isEnglish
            ? $"Targeted remediation (Score: {w.LowestScore}/100 in {w.Source})"
            : $"Củng cố điểm yếu ({w.LowestScore}/100 từ {w.Source})"))
            .ToList();

        if (searchSeeds.Count == 0)
        {
            if (snapshot.TopAiTopics.Count > 0)
            {
                foreach (var kv in snapshot.TopAiTopics.Take(2))
                {
                    searchSeeds.Add((kv.Key, false, isEnglish
                        ? $"Popular in live Atus learner stream ({kv.Value} queries)"
                        : $"Chủ đề được hỏi nhiều nhất trên hệ thống ({kv.Value} lượt)"));
                }
            }
            else
            {
                searchSeeds.Add(("async await ThreadPool Task", false, isEnglish
                    ? "High-impact core .NET backend mastery"
                    : "Kiến thức cốt lõi bắt buộc khi phỏng vấn .NET"));
                searchSeeds.Add(("Dependency Injection Clean Architecture", false, isEnglish
                    ? "Essential enterprise design architecture"
                    : "Nền tảng thiết kế kiến trúc hệ thống Enterprise"));
            }
        }

        foreach (var seed in searchSeeds.Take(3))
        {
            var searchResult = await _semanticSearch.SearchAsync(seed.Query, isEnglish, topK: 2, ct);
            foreach (var hit in searchResult.Hits)
            {
                var url = isEnglish ? $"/csharp-curriculum/{hit.LessonNumber}" : $"/chuong-trinh-csharp/{hit.LessonNumber}";
                if (!seenUrls.Add(url)) continue;

                recommendedLessons.Add(new AdaptiveRecommendationItem(
                    Title: hit.Title,
                    Url: url,
                    Reason: seed.ReasonPrefix,
                    Category: seed.IsWeakSpot ? (isEnglish ? "Adaptive Remediation" : "Bù đắp lỗ hổng") : (isEnglish ? "Core Path" : "Lộ trình trọng tâm"),
                    RelevanceScore: hit.Score,
                    RecommendedDifficulty: hit.SectionNumber >= 5 ? "Senior" : "Intermediate"
                ));

                if (recommendedLessons.Count >= 4) break;
            }
            if (recommendedLessons.Count >= 4) break;
        }

        string suggestedLevel = DetermineSuggestedInterviewLevel(weakSpots, isEnglish);
        var trendingQuestions = snapshot.TopAiTopics.Count > 0
            ? snapshot.TopAiTopics.Keys.Take(4).ToList()
            : (isEnglish
                ? ["Async/Await & ThreadPool", "Entity Framework Core", "Dependency Injection & Lifetimes", "Clean Architecture & DDD"]
                : ["Async/Await & ThreadPool", "Entity Framework Core", "Dependency Injection & Lifetimes", "Clean Architecture & DDD"]);

        return new AdaptiveLearningProfileResponse(
            IsAuthenticated: isAuth,
            DetectedWeakSpots: weakSpots,
            RecommendedLessons: recommendedLessons,
            TrendingQuestions: trendingQuestions,
            SuggestedInterviewLevel: suggestedLevel,
            GeneratedAt: DateTime.UtcNow
        );
    }

    private static string DetermineSuggestedInterviewLevel(List<AdaptiveWeakSpotDto> weakSpots, bool isEnglish)
    {
        if (weakSpots.Count == 0)
        {
            return isEnglish ? "Middle / Intermediate" : "Middle / Trung cấp";
        }

        var minScore = weakSpots.Min(w => w.LowestScore);
        if (minScore < 55)
        {
            return isEnglish ? "Junior (Foundational Focus)" : "Junior (Củng cố nền tảng)";
        }

        if (minScore < 75)
        {
            return isEnglish ? "Mid-Level (.NET Internals)" : "Middle (Đào sâu bản chất .NET)";
        }

        return isEnglish ? "Senior Architect" : "Senior / Tech Lead";
    }
}
