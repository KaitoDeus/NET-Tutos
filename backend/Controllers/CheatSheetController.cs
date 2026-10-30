using NET_Tutos.Services;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class CheatSheetController : Controller
{
    private readonly ITutorialService _tutorialService;

    public CheatSheetController(ITutorialService tutorialService)
    {
        _tutorialService = tutorialService;
    }

    public async Task<IActionResult> Index(string? category)
    {
        var snippets = await _tutorialService.GetCodeSnippetsAsync(category);
        ViewBag.SelectedCategory = category;
        return View(snippets);
    }
}

