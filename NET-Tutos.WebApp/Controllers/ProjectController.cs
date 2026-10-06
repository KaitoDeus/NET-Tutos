using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class ProjectController : Controller
{
    private readonly ICapstoneProjectService _projectService;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(
        ICapstoneProjectService projectService,
        ILogger<ProjectController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId, DifficultyLevel? level)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var viewModel = await _projectService.GetProjectsListAsync(categoryId, level, userId);
        return View(viewModel);
    }

    [HttpGet]
    [Route("Project/Details/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var canonicalSlug = LocalizationHelper.GetCanonicalSlug(slug);
        var viewModel = await _projectService.GetProjectDetailsAsync(canonicalSlug, userId)
                     ?? await _projectService.GetProjectDetailsAsync(slug, userId);

        if (viewModel == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitProjectInputModel model)
    {
        var project = await _projectService.GetProjectByIdAsync(model.ProjectId);
        if (project == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đồ án tương ứng.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin nộp bài không hợp lệ. Vui lòng kiểm tra lại URL GitHub và ghi chú.";
            return RedirectToAction(nameof(Details), new { slug = project.Slug });
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, message, _) = await _projectService.SubmitProjectAsync(userId, model);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Details), new { slug = project.Slug });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyProjects()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var viewModel = await _projectService.GetUserProjectsAsync(userId);
        return View(viewModel);
    }
}
