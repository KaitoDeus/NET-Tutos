using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class RoadmapStep
{
    public int StepNumber { get; set; }
    public int CategoryId { get; set; }
    public string CategorySlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DifficultyLevel Level { get; set; }
    public string BadgeColor { get; set; } = "primary";
    public string Icon { get; set; } = "bi-code-square";
    public List<string> KeyTopics { get; set; } = new();
    public List<Tutorial> RelatedTutorials { get; set; } = new();
    public bool IsUnlocked { get; set; } = true;
    public bool IsCompleted { get; set; }
    public int CompletedLessonsCount { get; set; }
    public int TotalLessonsCount { get; set; }
    public string? BlockingStageTitleVi { get; set; }
    public string? BlockingStageTitleEn { get; set; }
    public string? NextPrerequisiteTutorialSlug { get; set; }
}

public class RoadmapViewModel
{
    public List<RoadmapStep> Steps { get; set; } = new();
    public bool IsAuthenticated { get; set; }
    public bool BypassStageLocks { get; set; }
    public HashSet<int> CompletedTutorialIds { get; set; } = new();
}

