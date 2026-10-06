using NET_Tutos.Models.Entities;

namespace NET_Tutos.Services;

public interface ITutorialService
{
    Task<IEnumerable<Category>> GetCategoriesWithTutorialsAsync();
    Task<IEnumerable<Tutorial>> GetFeaturedTutorialsAsync(int count = 6);
    Task<IEnumerable<Tutorial>> GetRecentTutorialsAsync(int count = 6);
    Task<(IEnumerable<Tutorial> Items, int TotalCount)> GetTutorialsAsync(string? categorySlug, DifficultyLevel? difficulty, string? search, int page = 1, int pageSize = 12);
    Task<Tutorial?> GetTutorialBySlugAsync(string slug);
    Task<Tutorial?> GetTutorialByIdAsync(int id);
    Task<Tutorial?> GetNextTutorialAsync(int currentTutorialId);
    Task<Tutorial?> GetPreviousTutorialAsync(int currentTutorialId);
    Task IncrementViewCountAsync(int tutorialId);
    Task<List<QuizQuestion>> GetQuizQuestionsByTutorialIdAsync(int tutorialId);
    Task<List<QuizQuestion>> GetRandomQuizQuestionsAsync(int count = 10, DifficultyLevel? level = null);
    Task<List<CodeSnippet>> GetCodeSnippetsAsync(string? category = null);
    Task<int> GetTotalTutorialsCountAsync();
    Task<int> GetTotalCategoriesCountAsync();
    Task<int> GetTotalQuizzesCountAsync();
}

