using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;

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

public partial class CategoryItem : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "primary";
    public string IconClass { get; set; } = "bi-book";
    public int TutorialCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChipBgColor))]
    [NotifyPropertyChangedFor(nameof(ChipBorderColor))]
    [NotifyPropertyChangedFor(nameof(ChipTextColor))]
    private bool isSelected;

    public string IconEmoji => Slug switch
    {
        var s when s.Contains("co-ban") || s.Contains("basic") => "💻",
        var s when s.Contains("oop") || s.Contains("nang-cao") => "🧩",
        var s when s.Contains("entity") || s.Contains("database") || s.Contains("ef") => "🗄️",
        var s when s.Contains("aspnet") || s.Contains("web") || s.Contains("api") => "🌐",
        _ => "📘"
    };

    public string GradientStart => Slug switch
    {
        var s when s.Contains("co-ban") || s.Contains("basic") => "#0284C7", // Sky blue
        var s when s.Contains("oop") || s.Contains("nang-cao") => "#7C3AED", // Violet
        var s when s.Contains("entity") || s.Contains("database") || s.Contains("ef") => "#059669", // Emerald
        var s when s.Contains("aspnet") || s.Contains("web") || s.Contains("api") => "#E11D48", // Rose
        _ => "#4F46E5"
    };

    public string GradientEnd => Slug switch
    {
        var s when s.Contains("co-ban") || s.Contains("basic") => "#06B6D4",
        var s when s.Contains("oop") || s.Contains("nang-cao") => "#A855F7",
        var s when s.Contains("entity") || s.Contains("database") || s.Contains("ef") => "#10B981",
        var s when s.Contains("aspnet") || s.Contains("web") || s.Contains("api") => "#F97316",
        _ => "#818CF8"
    };

    public Color ChipBgColor => IsSelected ? Color.FromArgb("#4F46E5") : Color.FromArgb("#1E293B");
    public Color ChipBorderColor => IsSelected ? Color.FromArgb("#818CF8") : Color.FromArgb("#334155");
    public Color ChipTextColor => IsSelected ? Colors.White : Color.FromArgb("#CBD5E1");
    public Color IconBgColor => Color.FromArgb(GradientStart);
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

    public string ReadingTimeDisplay => $"⏱️ {EstimatedReadingMinutes} phút";
    public string StatusBadge => IsCompleted ? "✓ Đã học" : "";
    public bool IsNotCompleted => !IsCompleted;

    // Vibrant Difficulty Styling
    public string DifficultyText => Difficulty?.ToLower() switch
    {
        "beginner" => "🌱 Cơ bản",
        "intermediate" => "⚡ Trung cấp",
        "advanced" => "🔥 Nâng cao",
        _ => Difficulty ?? "Cơ bản"
    };

    public Color DifficultyColor => Difficulty?.ToLower() switch
    {
        "beginner" => Color.FromArgb("#34D399"),
        "intermediate" => Color.FromArgb("#FBBF24"),
        "advanced" => Color.FromArgb("#FB7185"),
        _ => Color.FromArgb("#38BDF8")
    };

    public Color DifficultyBgColor => Difficulty?.ToLower() switch
    {
        "beginner" => Color.FromArgb("#064E3B"),
        "intermediate" => Color.FromArgb("#451A03"),
        "advanced" => Color.FromArgb("#4C0519"),
        _ => Color.FromArgb("#082F49")
    };

    public Color DifficultyBorderColor => Difficulty?.ToLower() switch
    {
        "beginner" => Color.FromArgb("#059669"),
        "intermediate" => Color.FromArgb("#D97706"),
        "advanced" => Color.FromArgb("#BE123C"),
        _ => Color.FromArgb("#0284C7")
    };

    // Category Color & Icon
    public Color CategoryAccentColor => (CategoryId % 4) switch
    {
        1 => Color.FromArgb("#38BDF8"),
        2 => Color.FromArgb("#C084FC"),
        3 => Color.FromArgb("#34D399"),
        0 => Color.FromArgb("#FB923C"),
        _ => Color.FromArgb("#818CF8")
    };

    public Color CategoryBgColor => (CategoryId % 4) switch
    {
        1 => Color.FromArgb("#0C4A6E"),
        2 => Color.FromArgb("#3B0764"),
        3 => Color.FromArgb("#064E3B"),
        0 => Color.FromArgb("#431407"),
        _ => Color.FromArgb("#1E1B4B")
    };

    public Color CategoryBorderColor => (CategoryId % 4) switch
    {
        1 => Color.FromArgb("#0284C7"),
        2 => Color.FromArgb("#7C3AED"),
        3 => Color.FromArgb("#059669"),
        0 => Color.FromArgb("#EA580C"),
        _ => Color.FromArgb("#4F46E5")
    };

    public string CategoryIcon => (CategoryId % 4) switch
    {
        1 => "💻",
        2 => "🧩",
        3 => "🗄️",
        0 => "🌐",
        _ => "📘"
    };
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

    public string DifficultyText => Difficulty?.ToLower() switch
    {
        "beginner" => "🌱 Cơ bản",
        "intermediate" => "⚡ Trung cấp",
        "advanced" => "🔥 Nâng cao",
        _ => Difficulty ?? "Cơ bản"
    };

    public Color DifficultyColor => Difficulty?.ToLower() switch
    {
        "beginner" => Color.FromArgb("#34D399"),
        "intermediate" => Color.FromArgb("#FBBF24"),
        "advanced" => Color.FromArgb("#FB7185"),
        _ => Color.FromArgb("#38BDF8")
    };

    public Color DifficultyBgColor => Difficulty?.ToLower() switch
    {
        "beginner" => Color.FromArgb("#064E3B"),
        "intermediate" => Color.FromArgb("#451A03"),
        "advanced" => Color.FromArgb("#4C0519"),
        _ => Color.FromArgb("#082F49")
    };

    public Color CategoryAccentColor => (CategoryId % 4) switch
    {
        1 => Color.FromArgb("#38BDF8"),
        2 => Color.FromArgb("#C084FC"),
        3 => Color.FromArgb("#34D399"),
        0 => Color.FromArgb("#FB923C"),
        _ => Color.FromArgb("#818CF8")
    };

    public Color CategoryBgColor => (CategoryId % 4) switch
    {
        1 => Color.FromArgb("#0C4A6E"),
        2 => Color.FromArgb("#3B0764"),
        3 => Color.FromArgb("#064E3B"),
        0 => Color.FromArgb("#431407"),
        _ => Color.FromArgb("#1E1B4B")
    };
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

    public Color ModuleAccentColor => (ModuleNumber % 4) switch
    {
        1 => Color.FromArgb("#38BDF8"),
        2 => Color.FromArgb("#A78BFA"),
        3 => Color.FromArgb("#34D399"),
        0 => Color.FromArgb("#FB923C"),
        _ => Color.FromArgb("#818CF8")
    };

    public Color ModuleBgColor => (ModuleNumber % 4) switch
    {
        1 => Color.FromArgb("#0C4A6E"),
        2 => Color.FromArgb("#3B0764"),
        3 => Color.FromArgb("#064E3B"),
        0 => Color.FromArgb("#431407"),
        _ => Color.FromArgb("#1E1B4B")
    };

    public string ModuleIcon => (ModuleNumber % 4) switch
    {
        1 => "🚀",
        2 => "🧩",
        3 => "🗄️",
        0 => "🌐",
        _ => "📍"
    };
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
