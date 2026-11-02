using System.Diagnostics;
using System.Security.Claims;
using NET_Tutos.Models;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class HomeController : Controller
{
    private readonly ITutorialService _tutorialService;
    private readonly DatabaseProviderInfo _dbInfo;
    private readonly ILearningProgressService _progressService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityFeedService _activityFeedService;

    public HomeController(
        ITutorialService tutorialService, 
        DatabaseProviderInfo dbInfo,
        ILearningProgressService progressService,
        UserManager<ApplicationUser> userManager,
        IActivityFeedService activityFeedService)
    {
        _tutorialService = tutorialService;
        _dbInfo = dbInfo;
        _progressService = progressService;
        _userManager = userManager;
        _activityFeedService = activityFeedService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _tutorialService.GetCategoriesWithTutorialsAsync();
        var featured = await _tutorialService.GetFeaturedTutorialsAsync(6);
        var recent = await _tutorialService.GetRecentTutorialsAsync(4);
        var totalTutorials = await _tutorialService.GetTotalTutorialsCountAsync();
        var totalCategories = await _tutorialService.GetTotalCategoriesCountAsync();
        var totalQuizzes = await _tutorialService.GetTotalQuizzesCountAsync();

        var viewModel = new HomeViewModel
        {
            Categories = categories,
            FeaturedTutorials = featured,
            RecentTutorials = recent,
            TotalTutorials = totalTutorials,
            TotalCategories = totalCategories,
            TotalQuizzes = totalQuizzes,
            DatabaseProviderUsed = _dbInfo.Name,
            DatabaseProviderUsedEn = _dbInfo.NameEn,
            RecentActivities = await _activityFeedService.GetRecentActivitiesAsync(take: 4)
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                viewModel.LastAccessedTutorial = await _progressService.GetLastAccessedTutorialAsync(userId);
                var user = await _userManager.FindByIdAsync(userId);
                viewModel.UserExperiencePoints = user?.ExperiencePoints;
                var completedIds = await _progressService.GetCompletedLessonIdsAsync(userId);
                viewModel.CompletedLessonsCount = completedIds.Count;
            }
        }

        return View(viewModel);
    }

    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

