using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class TutorialDetailViewModel
{
    public Tutorial Tutorial { get; set; } = null!;
    public string RenderedHtmlContent { get; set; } = string.Empty;
    public Tutorial? PreviousTutorial { get; set; }
    public Tutorial? NextTutorial { get; set; }
    public IEnumerable<Tutorial> RelatedTutorials { get; set; } = new List<Tutorial>();
}

public class TutorialListViewModel
{
    public IEnumerable<Tutorial> Tutorials { get; set; } = new List<Tutorial>();
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    public string? CurrentCategorySlug { get; set; }
    public Category? CurrentCategory { get; set; }
    public DifficultyLevel? CurrentDifficulty { get; set; }
    public string? SearchQuery { get; set; }
    public int TotalCount { get; set; }
}

