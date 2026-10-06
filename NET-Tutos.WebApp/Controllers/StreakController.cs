using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class StreakController : Controller
{
    private readonly IStreakService _streakService;
    private readonly UserManager<ApplicationUser> _userManager;

    public StreakController(
        IStreakService streakService,
        UserManager<ApplicationUser> userManager)
    {
        _streakService = streakService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var userId = _userManager.GetUserId(User);
        var status = await _streakService.GetUserStreakStatusAsync(userId);
        return Json(status);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để điểm danh." });
        }

        var result = await _streakService.CheckInTodayAsync(userId);
        return Json(result);
    }
}
