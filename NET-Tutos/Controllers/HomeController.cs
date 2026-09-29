using System.Diagnostics;
using NET_Tutos.Models;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class HomeController : Controller
{
    private readonly ITutorialService _tutorialService;
    private readonly DatabaseProviderInfo _dbInfo;

    public HomeController(ITutorialService tutorialService, DatabaseProviderInfo dbInfo)
    {
        _tutorialService = tutorialService;
        _dbInfo = dbInfo;
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
            DatabaseProviderUsed = _dbInfo.Name
        };

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

