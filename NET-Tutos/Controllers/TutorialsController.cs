using System.Security.Claims;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class TutorialsController : Controller
{
    private readonly ITutorialService _tutorialService;
    private readonly IMarkdownService _markdownService;
    private readonly ILearningProgressService _progressService;
    private readonly IDiscussionService _discussionService;

    public TutorialsController(
        ITutorialService tutorialService, 
        IMarkdownService markdownService,
        ILearningProgressService progressService,
        IDiscussionService discussionService)
    {
        _tutorialService = tutorialService;
        _markdownService = markdownService;
        _progressService = progressService;
        _discussionService = discussionService;
    }

    // GET: /Tutorials
    public async Task<IActionResult> Index(string? category, DifficultyLevel? level, string? search, int page = 1)
    {
        const int pageSize = 9;
        var (items, totalCount) = await _tutorialService.GetTutorialsAsync(category, level, search, page, pageSize);
        var categories = await _tutorialService.GetCategoriesWithTutorialsAsync();

        var currentCategory = !string.IsNullOrWhiteSpace(category) 
            ? categories.FirstOrDefault(c => c.Slug == category) 
            : null;

        var viewModel = new TutorialListViewModel
        {
            Tutorials = items,
            Categories = categories,
            CurrentCategorySlug = category,
            CurrentCategory = currentCategory,
            CurrentDifficulty = level,
            SearchQuery = search,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    // GET: /bai-hoc/{slug}
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var tutorial = await _tutorialService.GetTutorialBySlugAsync(slug);
        if (tutorial == null)
        {
            return NotFound();
        }

        // Increment view count asynchronously in background
        _ = _tutorialService.IncrementViewCountAsync(tutorial.Id);

        var previous = await _tutorialService.GetPreviousTutorialAsync(tutorial.Id);
        var next = await _tutorialService.GetNextTutorialAsync(tutorial.Id);
        bool isEn = HttpContext.IsEnglish();
        var markdownContent = tutorial.GetContentMarkdown(isEn);
        var renderedHtml = _markdownService.ToHtml(markdownContent);

        var viewModel = new TutorialDetailViewModel
        {
            Tutorial = tutorial,
            RenderedHtmlContent = renderedHtml,
            PreviousTutorial = previous,
            NextTutorial = next
        };

        string? currentUserId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                viewModel.IsCompletedByCurrentUser = await _progressService.IsLessonCompletedAsync(currentUserId, tutorial.Id);
                _ = _progressService.RecordLessonAccessAsync(currentUserId, tutorial.Id);
            }
        }

        var comments = await _discussionService.GetCommentsAsync("Tutorial", tutorial.Id, currentUserId);
        viewModel.Discussion = new DiscussionSectionViewModel
        {
            TopicType = "Tutorial",
            TopicId = tutorial.Id,
            TopicTitle = tutorial.Title,
            Comments = comments,
            TotalCommentsCount = comments.Count + comments.Sum(c => c.Replies.Count),
            CurrentUserId = currentUserId,
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
            IsAdmin = User.IsInRole("Admin")
        };

        return View(viewModel);
    }
}

