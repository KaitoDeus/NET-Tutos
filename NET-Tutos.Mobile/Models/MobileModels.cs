namespace NET_Tutos.Mobile.Models;

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int TotalXp { get; set; }
    public int Level { get; set; } = 1;
    public int StreakDays { get; set; }
    public int CompletedLessonsCount { get; set; }
}

public class CategoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "primary";
    public string IconClass { get; set; } = "bi-book";
    public int TutorialCount { get; set; }
}

public class TutorialSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int EstimatedReadingMinutes { get; set; }
    public string Difficulty { get; set; } = "Beginner";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int ViewCount { get; set; }
    public bool IsCompleted { get; set; }

    public string ReadingTimeDisplay => $"{EstimatedReadingMinutes} phút đọc";
    public string StatusBadge => IsCompleted ? "✓ Đã hoàn thành" : "Chưa hoàn thành";
}

public class TutorialDetail
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public int EstimatedReadingMinutes { get; set; }
    public string Difficulty { get; set; } = "Beginner";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<QuizItem> Quizzes { get; set; } = new();
    public TutorialSummary? PreviousTutorial { get; set; }
    public TutorialSummary? NextTutorial { get; set; }
}

public class QuizItem
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = "A";
    public string Explanation { get; set; } = string.Empty;

    public string SelectedOption { get; set; } = string.Empty;
    public bool IsAnswered { get; set; }
    public bool IsCorrect => SelectedOption == CorrectOption;
}

public class RoadmapModule
{
    public int ModuleNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public List<TutorialSummary> Lessons { get; set; } = new();

    public double ProgressPercent => TotalLessons > 0 ? (double)CompletedLessons / TotalLessons : 0;
    public string ProgressText => $"{CompletedLessons}/{TotalLessons} bài ({Math.Round(ProgressPercent * 100)}%)";
}

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserProfile? User { get; set; }
}

public class CompleteLessonResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int XpEarned { get; set; }
    public int TotalXp { get; set; }
    public int StreakDays { get; set; }
}

public class StreakInfo
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public bool IsActiveToday { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public List<bool> Last7DaysActive { get; set; } = new();
}
