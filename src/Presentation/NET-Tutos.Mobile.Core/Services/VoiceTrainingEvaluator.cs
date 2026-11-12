using NET_Tutos.Mobile.Core.Models;

namespace NET_Tutos.Mobile.Core.Services;

/// <summary>
/// Deterministic offline rubric scorer for spoken technical interview drills when the device has no connectivity.
/// </summary>
public static class VoiceTrainingEvaluator
{
    private sealed record KeywordRule(string[] Keywords, int Weight, string StrengthVi, string StrengthEn, string MissingVi, string MissingEn);

    private static readonly KeywordRule[] AsyncRules =
    [
        new(["non-blocking", "không block", "không chặn", "threadpool"], 18,
            "Giải thích đúng cơ chế non-blocking và giải phóng ThreadPool.",
            "Accurately explained non-blocking execution and ThreadPool release.",
            "Nên nhấn mạnh async/await giúp giải phóng luồng về ThreadPool.",
            "Mention how async/await releases worker threads back to the ThreadPool."),
        new(["state machine", "máy trạng thái", "continuation"], 20,
            "Nhắc tới Roslyn compiler tạo struct State Machine cho async/await.",
            "Referenced Roslyn compiler generating an async State Machine struct.",
            "Bổ sung cơ chế compiler sinh State Machine sẽ đạt mức Senior.",
            "Mentioning compiler-generated State Machine shows Senior-level depth."),
        new(["cancellationtoken", "deadlock", "configureawait", ".result"], 18,
            "Đề cập lưu ý thực chiến về CancellationToken và tránh deadlock.",
            "Covered real-world caveats like CancellationToken and deadlock avoidance.",
            "Cần nêu tác hại của .Result/.Wait() và cách truyền CancellationToken.",
            "Discuss CancellationToken propagation and why .Result/.Wait() can deadlock.")
    ];

    private static readonly KeywordRule[] EfCoreRules =
    [
        new(["asnotracking", "change tracker", "theo dõi"], 18,
            "Hiểu rõ Change Tracker và cách dùng AsNoTracking cho truy vấn chỉ đọc.",
            "Explained EF Core Change Tracker overhead and AsNoTracking optimization.",
            "Nhắc tới AsNoTracking() cho truy vấn chỉ đọc để giảm cấp phát RAM.",
            "Mention AsNoTracking() for read-only queries to reduce tracking overhead."),
        new(["n+1", "include", "eager", "projection", "select"], 20,
            "Phân tích được bẫy truy vấn N+1 và cách khắc phục với Include/Select.",
            "Addressed N+1 query pitfalls and mitigation with Include or projections.",
            "Giải thích cách tránh N+1 bằng .Include() hoặc Projection .Select().",
            "Explain preventing N+1 queries using .Include() or .Select() projections."),
        new(["iqueryable", "ienumerable", "sql"], 16,
            "Phân biệt đúng IQueryable chạy phía DB vs IEnumerable chạy trên RAM.",
            "Distinguished database-side IQueryable from in-memory IEnumerable.",
            "So sánh IQueryable (SQL translation) với IEnumerable (in-memory).",
            "Contrast server-side IQueryable translation with in-memory IEnumerable.")
    ];

    private static readonly KeywordRule[] GeneralDotNetRules =
    [
        new(["stack", "heap", "gc", "garbage collection", "value type", "reference type"], 18,
            "Trình bày chính xác vùng nhớ Stack/Heap và vòng đời Garbage Collector.",
            "Accurately covered Stack vs Heap allocation and GC lifecycle.",
            "Làm rõ phân bổ bộ nhớ Stack/Heap hoặc các thế hệ GC.",
            "Clarify Stack vs Heap allocation and .NET GC generations."),
        new(["dependency injection", "ioc", "interface", "solid", "scoped", "singleton", "transient"], 18,
            "Vận dụng tốt Dependency Injection, vòng đời DI và nguyên lý SOLID.",
            "Demonstrated strong grasp of DI lifetimes and SOLID design principles.",
            "Liên hệ thêm vòng đời DI (Scoped/Transient/Singleton) hoặc SOLID.",
            "Connect answer to DI service lifetimes or SOLID principles."),
        new(["idisposable", "using", "benchmark", "index", "cache", "redis"], 18,
            "Có góc nhìn tối ưu hiệu năng thực tế (tài nguyên, index hoặc caching).",
            "Included practical performance considerations such as disposal, indexing, or caching.",
            "Nêu thêm ví dụ thực tế về hiệu năng hoặc xử lý tài nguyên.",
            "Add a concrete production performance or resource cleanup example.")
    ];

    public static VoiceEvaluationResult EvaluateOffline(VoiceEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        bool isEn = string.Equals(request.Language, "en", StringComparison.OrdinalIgnoreCase) ||
                    request.Language.StartsWith("en-", StringComparison.OrdinalIgnoreCase);

        var transcript = (request.Transcript ?? string.Empty).Trim();
        var lower = transcript.ToLowerInvariant();

        if (transcript.Length < 15)
        {
            var briefFeedback = isEn
                ? "Your spoken answer was too brief. Explain the core concept, internal mechanism, and a practical trade-off."
                : "Câu trả lời nói còn quá ngắn. Hãy trình bày rõ khái niệm cốt lõi, cơ chế bên dưới và ví dụ thực tế.";

            return new VoiceEvaluationResult
            {
                IsSuccess = true,
                FlashcardId = request.FlashcardId,
                OverallScore = 25,
                TechnicalAccuracyScore = 20,
                ClarityStructureScore = 30,
                DepthPracticalScore = 20,
                PerformanceRating = isEn ? "Too Brief (Offline)" : "Chưa đủ ý (Offline)",
                Feedback = briefFeedback,
                Strengths = [isEn ? "Attempted voice response." : "Đã hoàn thành lượt luyện nói."],
                MissingPoints =
                [
                    isEn ? "Explain the core technical concept clearly." : "Trình bày khái niệm cốt lõi.",
                    isEn ? "Include internal runtime details or trade-offs." : "Nêu cơ chế hoạt động bên dưới hoặc lưu ý hiệu năng."
                ],
                FollowUpQuestion = isEn
                    ? "Can you walk through a real production example in ASP.NET Core?"
                    : "Bạn có thể đưa ra một ví dụ thực tế trong dự án ASP.NET Core không?",
                SpokenSummary = briefFeedback,
                IsOfflineEvaluation = true
            };
        }

        var selectedRules = SelectRuleSet(request.Topic, lower);
        var strengths = new List<string>();
        var missing = new List<string>();
        int bonus = 0;

        foreach (var rule in selectedRules)
        {
            if (rule.Keywords.Any(k => lower.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                bonus += rule.Weight;
                strengths.Add(isEn ? rule.StrengthEn : rule.StrengthVi);
            }
            else
            {
                missing.Add(isEn ? rule.MissingEn : rule.MissingVi);
            }
        }

        int wordCount = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        int clarityBoost = wordCount >= 35 ? 16 : wordCount >= 18 ? 10 : 4;
        int baseScore = Math.Clamp(38 + bonus + clarityBoost, 30, 96);
        int accuracy = Math.Clamp(35 + bonus + 6, 30, 98);
        int clarity = Math.Clamp(52 + clarityBoost * 2, 40, 95);
        int depth = Math.Clamp(34 + bonus + (wordCount >= 25 ? 12 : 4), 25, 95);

        string rating = baseScore switch
        {
            >= 85 => isEn ? "Strong Senior Candidate" : "Xuất sắc (Chuẩn Senior)",
            >= 70 => isEn ? "Solid Mid-Level Candidate" : "Tốt (Đạt chuẩn Mid/Senior)",
            >= 55 => isEn ? "Developing Understanding" : "Khá (Cần đào sâu cơ chế)",
            _ => isEn ? "Needs Deeper Technical Detail" : "Cần bổ sung kiến thức chuyên sâu"
        };

        string feedback = isEn
            ? $"Offline Voice Rubric Score: {baseScore}/100. You covered {strengths.Count} key technical pillar(s)."
            : $"Đánh giá luyện nói (Offline): {baseScore}/100 điểm. Bạn đã chạm {strengths.Count} trụ cột kỹ thuật quan trọng.";

        string followUp = missing.Count > 0
            ? (isEn ? $"Follow-up: How would you address: {missing[0]}" : $"Gợi ý đào sâu: {missing[0]}")
            : (isEn ? "Excellent depth! What trade-offs occur under high concurrency?" : "Rất tốt! Khi tải cao (high concurrency) cần lưu ý đánh đổi gì?");

        if (strengths.Count == 0)
        {
            strengths.Add(isEn ? "Clear spoken structure and delivery." : "Trình bày mạch lạc, tự tin bằng giọng nói.");
        }

        string spokenSummary = isEn
            ? $"Score {baseScore} out of 100. {rating}. {feedback} {followUp}"
            : $"Điểm đánh giá {baseScore} trên 100. {rating}. {feedback} {followUp}";

        return new VoiceEvaluationResult
        {
            IsSuccess = true,
            FlashcardId = request.FlashcardId,
            OverallScore = baseScore,
            TechnicalAccuracyScore = accuracy,
            ClarityStructureScore = clarity,
            DepthPracticalScore = depth,
            PerformanceRating = rating,
            Feedback = feedback,
            Strengths = strengths,
            MissingPoints = missing,
            FollowUpQuestion = followUp,
            SpokenSummary = spokenSummary,
            IsOfflineEvaluation = true
        };
    }

    private static KeywordRule[] SelectRuleSet(string topic, string lowerTranscript)
    {
        if (topic.Contains("Async", StringComparison.OrdinalIgnoreCase) ||
            lowerTranscript.Contains("async", StringComparison.OrdinalIgnoreCase) ||
            lowerTranscript.Contains("task", StringComparison.OrdinalIgnoreCase))
        {
            return AsyncRules;
        }

        if (topic.Contains("EfCore", StringComparison.OrdinalIgnoreCase) ||
            lowerTranscript.Contains("iqueryable", StringComparison.OrdinalIgnoreCase) ||
            lowerTranscript.Contains("ef core", StringComparison.OrdinalIgnoreCase))
        {
            return EfCoreRules;
        }

        return GeneralDotNetRules;
    }
}
