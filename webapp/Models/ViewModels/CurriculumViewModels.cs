using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class CurriculumLessonViewModel
{
    public int LessonNumber { get; set; }
    public int SectionNumber { get; set; }
    public string SectionTitleVi { get; set; } = string.Empty;
    public string SectionTitleEn { get; set; } = string.Empty;
    public string SectionBadgeColor { get; set; } = "primary";
    public string SectionIcon { get; set; } = "bi-folder2-open";

    public string TitleVi { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string SummaryVi { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;
    public int EstimatedMinutes { get; set; } = 15;
    public List<string> KeyTags { get; set; } = new();

    public string SampleCode { get; set; } = string.Empty;
    public List<string> LearningOutcomesVi { get; set; } = new();
    public List<string> LearningOutcomesEn { get; set; } = new();

    public string? MatchingTutorialSlug { get; set; }
    public string? PlaygroundStarterCode { get; set; }

    public string GetTitle(bool isEn) => isEn ? TitleEn : TitleVi;
    public string GetSummary(bool isEn) => isEn ? SummaryEn : SummaryVi;
    public string GetSectionTitle(bool isEn) => isEn ? SectionTitleEn : SectionTitleVi;
    public List<string> GetLearningOutcomes(bool isEn) => isEn ? LearningOutcomesEn : LearningOutcomesVi;
}

public class CurriculumSectionViewModel
{
    public int SectionNumber { get; set; }
    public string TitleVi { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionVi { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "primary";
    public string Icon { get; set; } = "bi-laptop";
    public DifficultyLevel Level { get; set; } = DifficultyLevel.Beginner;
    public int LessonCount => Lessons.Count;
    public int TotalEstimatedHours { get; set; }
    public List<CurriculumLessonViewModel> Lessons { get; set; } = new();

    public string GetTitle(bool isEn) => isEn ? TitleEn : TitleVi;
    public string GetDescription(bool isEn) => isEn ? DescriptionEn : DescriptionVi;
}

public class CurriculumIndexViewModel
{
    public List<CurriculumSectionViewModel> Sections { get; set; } = new();
    public List<CurriculumLessonViewModel> FilteredLessons { get; set; } = new();
    public int? SelectedSectionNumber { get; set; }
    public DifficultyLevel? SelectedDifficulty { get; set; }
    public string? SearchQuery { get; set; }
    public int TotalLessonsCount { get; set; }
    public int TotalSectionsCount { get; set; }
    public int TotalEstimatedHours { get; set; }
}

public class CurriculumLessonDetailViewModel
{
    public CurriculumLessonViewModel Lesson { get; set; } = new();
    public CurriculumLessonViewModel? PreviousLesson { get; set; }
    public CurriculumLessonViewModel? NextLesson { get; set; }
    public bool HasExistingTutorial { get; set; }
}
