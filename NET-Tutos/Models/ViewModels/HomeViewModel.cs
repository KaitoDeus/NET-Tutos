using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class HomeViewModel
{
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    public IEnumerable<Tutorial> FeaturedTutorials { get; set; } = new List<Tutorial>();
    public IEnumerable<Tutorial> RecentTutorials { get; set; } = new List<Tutorial>();
    public int TotalTutorials { get; set; }
    public int TotalCategories { get; set; }
    public int TotalQuizzes { get; set; }
    public string DatabaseProviderUsed { get; set; } = "SQL Server";
    public Tutorial? LastAccessedTutorial { get; set; }
    public int? UserExperiencePoints { get; set; }
    public int? CompletedLessonsCount { get; set; }
    public List<ActivityFeedItemViewModel> RecentActivities { get; set; } = new();
}

