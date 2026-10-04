using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class FlashcardItemViewModel
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public FlashcardTopic Topic { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string TopicBadgeClass { get; set; } = "primary";
    public string TopicIconClass { get; set; } = "bi-code-slash";
    public DifficultyLevel Difficulty { get; set; }
    public string DifficultyName { get; set; } = string.Empty;
    public string DifficultyBadgeClass { get; set; } = "info";

    public string Question { get; set; } = string.Empty;
    public string? QuickHint { get; set; }
    public string AnswerMarkdown { get; set; } = string.Empty;
    public string AnswerHtml { get; set; } = string.Empty;
    public string? InterviewTip { get; set; }

    public int BoxLevel { get; set; } = 1;
    public int ReviewCount { get; set; } = 0;
    public DateTime? NextReviewDate { get; set; }
    public bool IsDueForReview { get; set; } = true;
    public bool IsMastered { get; set; } = false;
    public DateTime? LastReviewedAt { get; set; }
}

public class TopicSummaryViewModel
{
    public FlashcardTopic Topic { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-cpu";
    public string ColorClass { get; set; } = "primary";
    public int TotalCards { get; set; }
    public int MasteredCards { get; set; }
    public int DueCards { get; set; }
    public int LearningCards => Math.Max(0, TotalCards - MasteredCards);
    public double MasteryPercentage => TotalCards > 0 ? Math.Round((double)MasteredCards / TotalCards * 100, 1) : 0;
}

public class InterviewIndexViewModel
{
    public List<TopicSummaryViewModel> Topics { get; set; } = new();
    public int TotalCards { get; set; }
    public int MasteredCards { get; set; }
    public int DueTodayCount { get; set; }
    public int TotalReviewsCompleted { get; set; }
    public double OverallMasteryPercentage => TotalCards > 0 ? Math.Round((double)MasteredCards / TotalCards * 100, 1) : 0;
    public int CurrentStreak { get; set; }
    public int UserXp { get; set; }

    public List<FlashcardItemViewModel> RecentCards { get; set; } = new();
    public FlashcardTopic? CurrentTopic { get; set; }
    public DifficultyLevel? CurrentDifficulty { get; set; }
    public string? SearchQuery { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public class StudySessionViewModel
{
    public FlashcardTopic? Topic { get; set; }
    public string TopicName { get; set; } = "Toàn bộ chủ đề";
    public DifficultyLevel? Difficulty { get; set; }
    public List<FlashcardItemViewModel> QueueCards { get; set; } = new();
    public int TotalInSession => QueueCards.Count;
    public int RemainingInSession => QueueCards.Count;
}

public class FlashcardReviewRequest
{
    public int FlashcardId { get; set; }
    public FlashcardRating Rating { get; set; }
}

public class FlashcardReviewResponse
{
    public bool Success { get; set; }
    public int FlashcardId { get; set; }
    public int NewBoxLevel { get; set; }
    public int NextReviewDays { get; set; }
    public int XpEarned { get; set; }
    public int TotalXp { get; set; }
    public bool IsMastered { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class MockInterviewViewModel
{
    public DifficultyLevel? Difficulty { get; set; }
    public int TimeLimitSeconds { get; set; } = 600; // 10 minutes default
    public List<FlashcardItemViewModel> Questions { get; set; } = new();
}

public class MockAnswerItemRequest
{
    public int FlashcardId { get; set; }
    public int Score { get; set; } // 0 - 100 self-evaluation score
}

public class SubmitMockInterviewRequest
{
    public List<MockAnswerItemRequest> Answers { get; set; } = new();
    public int TimeSpentSeconds { get; set; }
}

public class MockInterviewResultViewModel
{
    public int TotalQuestions { get; set; }
    public double AverageScore { get; set; }
    public int PassedCount { get; set; }
    public string ReadinessLevel { get; set; } = string.Empty;
    public string ReadinessBadgeClass { get; set; } = "success";
    public string FeedbackSummary { get; set; } = string.Empty;
    public int XpAwarded { get; set; }
    public int TimeSpentSeconds { get; set; }
    public List<MockQuestionResultItem> Results { get; set; } = new();
}

public class MockQuestionResultItem
{
    public FlashcardItemViewModel Flashcard { get; set; } = new();
    public int Score { get; set; }
    public bool IsPassed => Score >= 70;
}
