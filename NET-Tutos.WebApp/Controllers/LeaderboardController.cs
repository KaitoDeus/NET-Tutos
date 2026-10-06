using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class LeaderboardController : Controller
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeaderboardController(
        ILeaderboardService leaderboardService,
        UserManager<ApplicationUser> userManager)
    {
        _leaderboardService = leaderboardService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);
        var viewModel = await _leaderboardService.GetLeaderboardAsync(currentUserId, 50);
        return View(viewModel);
    }
}
