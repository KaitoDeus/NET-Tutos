using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace NET_Tutos.Services;

public class TutorialService : ITutorialService
{
    private readonly AppDbContext _context;

    public TutorialService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetCategoriesWithTutorialsAsync()
    {
        return await _context.Categories
            .Include(c => c.Tutorials.OrderBy(t => t.OrderIndex))
            .OrderBy(c => c.OrderIndex)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Tutorial>> GetFeaturedTutorialsAsync(int count = 6)
    {
        return await _context.Tutorials
            .Include(t => t.Category)
            .Where(t => t.IsFeatured)
            .OrderBy(t => t.OrderIndex)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Tutorial>> GetRecentTutorialsAsync(int count = 6)
    {
        return await _context.Tutorials
            .Include(t => t.Category)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(IEnumerable<Tutorial> Items, int TotalCount)> GetTutorialsAsync(
        string? categorySlug,
        DifficultyLevel? difficulty,
        string? search,
        int page = 1,
        int pageSize = 12)
    {
        var query = _context.Tutorials
            .Include(t => t.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(t => t.Category != null && t.Category.Slug == categorySlug);
        }

        if (difficulty.HasValue)
        {
            query = query.Where(t => t.Difficulty == difficulty.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchTerm) ||
                                     t.Summary.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(t => t.Category!.OrderIndex)
            .ThenBy(t => t.OrderIndex)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Tutorial?> GetTutorialBySlugAsync(string slug)
    {
        return await _context.Tutorials
            .Include(t => t.Category)
            .Include(t => t.QuizQuestions)
            .Include(t => t.CodeSnippets)
            .FirstOrDefaultAsync(t => t.Slug == slug);
    }

    public async Task<Tutorial?> GetTutorialByIdAsync(int id)
    {
        return await _context.Tutorials
            .Include(t => t.Category)
            .Include(t => t.QuizQuestions)
            .Include(t => t.CodeSnippets)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tutorial?> GetNextTutorialAsync(int currentTutorialId)
    {
        var current = await _context.Tutorials
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == currentTutorialId);

        if (current == null) return null;

        // Same category next tutorial
        var nextInCat = await _context.Tutorials
            .Where(t => t.CategoryId == current.CategoryId && t.OrderIndex > current.OrderIndex)
            .OrderBy(t => t.OrderIndex)
            .FirstOrDefaultAsync();

        if (nextInCat != null) return nextInCat;

        // Next category first tutorial
        var currentCatOrder = current.Category?.OrderIndex ?? 0;
        var nextCat = await _context.Categories
            .Where(c => c.OrderIndex > currentCatOrder)
            .OrderBy(c => c.OrderIndex)
            .FirstOrDefaultAsync();

        if (nextCat != null)
        {
            return await _context.Tutorials
                .Where(t => t.CategoryId == nextCat.Id)
                .OrderBy(t => t.OrderIndex)
                .FirstOrDefaultAsync();
        }

        return null;
    }

    public async Task<Tutorial?> GetPreviousTutorialAsync(int currentTutorialId)
    {
        var current = await _context.Tutorials
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == currentTutorialId);

        if (current == null) return null;

        // Same category previous tutorial
        var prevInCat = await _context.Tutorials
            .Where(t => t.CategoryId == current.CategoryId && t.OrderIndex < current.OrderIndex)
            .OrderByDescending(t => t.OrderIndex)
            .FirstOrDefaultAsync();

        if (prevInCat != null) return prevInCat;

        // Previous category last tutorial
        var currentCatOrder = current.Category?.OrderIndex ?? 0;
        var prevCat = await _context.Categories
            .Where(c => c.OrderIndex < currentCatOrder)
            .OrderByDescending(c => c.OrderIndex)
            .FirstOrDefaultAsync();

        if (prevCat != null)
        {
            return await _context.Tutorials
                .Where(t => t.CategoryId == prevCat.Id)
                .OrderByDescending(t => t.OrderIndex)
                .FirstOrDefaultAsync();
        }

        return null;
    }

    public async Task IncrementViewCountAsync(int tutorialId)
    {
        await _context.Tutorials
            .Where(t => t.Id == tutorialId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ViewCount, t => t.ViewCount + 1));
    }

    public async Task<List<QuizQuestion>> GetQuizQuestionsByTutorialIdAsync(int tutorialId)
    {
        return await _context.QuizQuestions
            .Where(q => q.TutorialId == tutorialId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<QuizQuestion>> GetRandomQuizQuestionsAsync(int count = 10, DifficultyLevel? level = null)
    {
        var query = _context.QuizQuestions.Include(q => q.Tutorial).AsQueryable();
        if (level.HasValue)
        {
            query = query.Where(q => q.Difficulty == level.Value);
        }

        return await query
            .OrderBy(r => EF.Functions.Random())
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<CodeSnippet>> GetCodeSnippetsAsync(string? category = null)
    {
        var query = _context.CodeSnippets.AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.CategoryName == category);
        }

        return await query.OrderBy(s => s.Title).AsNoTracking().ToListAsync();
    }

    public async Task<int> GetTotalTutorialsCountAsync() => await _context.Tutorials.CountAsync();
    public async Task<int> GetTotalCategoriesCountAsync() => await _context.Categories.CountAsync();
    public async Task<int> GetTotalQuizzesCountAsync() => await _context.QuizQuestions.CountAsync();
}

