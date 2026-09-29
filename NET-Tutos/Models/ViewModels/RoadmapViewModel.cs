using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class RoadmapStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DifficultyLevel Level { get; set; }
    public string BadgeColor { get; set; } = "primary";
    public string Icon { get; set; } = "bi-code-square";
    public List<string> KeyTopics { get; set; } = new();
    public List<Tutorial> RelatedTutorials { get; set; } = new();
}

public class RoadmapViewModel
{
    public List<RoadmapStep> Steps { get; set; } = new();
}

