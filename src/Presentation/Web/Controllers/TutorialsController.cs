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
        var canonicalCategory = !string.IsNullOrWhiteSpace(category) ? LocalizationHelper.GetCanonicalSlug(category) : category;
        var (items, totalCount) = await _tutorialService.GetTutorialsAsync(canonicalCategory, level, search, page, pageSize);
        var categories = await _tutorialService.GetCategoriesWithTutorialsAsync();

        var currentCategory = !string.IsNullOrWhiteSpace(category) 
            ? categories.FirstOrDefault(c => c.Slug == category || c.Slug == canonicalCategory) 
            : null;

        string? currentUserId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        bool bypassLocks = User.IsInRole("Admin");
        var stageProgression = await _progressService.GetStageProgressionAsync(currentUserId, bypassLocks);

        var viewModel = new TutorialListViewModel
        {
            Tutorials = items,
            Categories = categories,
            CurrentCategorySlug = category,
            CurrentCategory = currentCategory,
            CurrentDifficulty = level,
            SearchQuery = search,
            TotalCount = totalCount,
            StageProgression = stageProgression,
            BypassStageLocks = bypassLocks
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

        var canonicalSlug = LocalizationHelper.GetCanonicalSlug(slug);
        var tutorial = await _tutorialService.GetTutorialBySlugAsync(canonicalSlug) ?? await _tutorialService.GetTutorialBySlugAsync(slug);
        if (tutorial == null)
        {
            return NotFound();
        }

        bool isEn = HttpContext.IsEnglish();
        string? currentUserId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        bool bypassLocks = User.IsInRole("Admin");

        var currentStageStatus = await _progressService.CheckTutorialAccessAsync(currentUserId, tutorial.Id, bypassLocks);
        bool isLocked = currentStageStatus != null && !currentStageStatus.IsUnlocked;

        var previous = await _tutorialService.GetPreviousTutorialAsync(tutorial.Id);
        var next = await _tutorialService.GetNextTutorialAsync(tutorial.Id);

        bool isNextLocked = false;
        if (next != null)
        {
            var nextAccess = await _progressService.CheckTutorialAccessAsync(currentUserId, next.Id, bypassLocks);
            isNextLocked = nextAccess != null && !nextAccess.IsUnlocked;
        }

        string renderedHtml = string.Empty;
        if (!isLocked)
        {
            // Increment view count safely only for unlocked lessons
            try
            {
                await _tutorialService.IncrementViewCountAsync(tutorial.Id);
            }
            catch
            {
                // Non-critical tracking fallback
            }

            var markdownContent = tutorial.GetContentMarkdown(isEn);
            renderedHtml = _markdownService.ToHtml(markdownContent);
        }

        var viewModel = new TutorialDetailViewModel
        {
            Tutorial = tutorial,
            RenderedHtmlContent = renderedHtml,
            PreviousTutorial = previous,
            NextTutorial = next,
            IsLocked = isLocked,
            CurrentStageStatus = currentStageStatus,
            IsNextTutorialLocked = isNextLocked
        };

        if (!string.IsNullOrEmpty(currentUserId))
        {
            viewModel.IsCompletedByCurrentUser = await _progressService.IsLessonCompletedAsync(currentUserId, tutorial.Id);
            var completedIds = await _progressService.GetCompletedLessonIdsAsync(currentUserId);
            viewModel.CompletedTutorialIds = new HashSet<int>(completedIds);
            if (!isLocked)
            {
                try
                {
                    await _progressService.RecordLessonAccessAsync(currentUserId, tutorial.Id);
                }
                catch
                {
                    // Non-critical tracking fallback
                }
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

