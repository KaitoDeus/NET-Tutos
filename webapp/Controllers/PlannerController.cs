using System.Security.Claims;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class PlannerController : Controller
{
    private readonly IStudyPlannerService _plannerService;

    public PlannerController(IStudyPlannerService plannerService)
    {
        _plannerService = plannerService;
    }

    // GET: /ke-hoach-hoc-tap hoặc /study-planner
    public async Task<IActionResult> Index()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            // Anonymous guest view: return preview state with HasActivePlan = false
            var guestModel = new StudyPlannerDashboardViewModel
            {
                HasActivePlan = false,
                CurrentStreak = 0,
                LongestStreak = 0
            };
            return View(guestModel);
        }

        var model = await _plannerService.GetDashboardAsync(userId);
        return View(model);
    }

    // POST: /Planner/Create
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStudyPlanRequest request)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        await _plannerService.CreatePlanAsync(userId, request);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Planner/ToggleItem
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ToggleItem(int itemId)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _plannerService.ToggleItemCompletionAsync(userId, itemId);
        return Json(result);
    }

    // POST: /Planner/Reschedule
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        await _plannerService.ReschedulePlanAsync(userId);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Planner/Reset
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        await _plannerService.ResetPlanAsync(userId);
        return RedirectToAction(nameof(Index));
    }
}
