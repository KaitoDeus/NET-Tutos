using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class ProfileController : Controller
{
    private readonly IDeveloperPortfolioService _portfolioService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IDeveloperPortfolioService portfolioService, ILogger<ProfileController> logger)
    {
        _portfolioService = portfolioService;
        _logger = logger;
    }

    // GET: /u/{username}
    [HttpGet("/u/{username}")]
    public async Task<IActionResult> Showcase(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return RedirectToAction("Index", "Home");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isEn = HttpContext.IsEnglish();

        var portfolio = await _portfolioService.GetPortfolioByUsernameAsync(username, currentUserId, isEn);
        if (portfolio == null)
        {
            ViewBag.RequestedUsername = username;
            return View("ProfileNotFound");
        }

        ViewData["Title"] = isEn 
            ? $"{portfolio.User.FullName ?? username} | .NET Developer Portfolio" 
            : $"{portfolio.User.FullName ?? username} | Hồ Sơ Năng Lực Lập Trình Viên .NET";

        return View(portfolio);
    }

    // GET: /Profile/MyPortfolio (Quick redirect for logged in user)
    [HttpGet("/Profile/MyPortfolio")]
    [Authorize]
    public IActionResult MyPortfolio()
    {
        var username = User.Identity?.Name?.Split('@')[0] ?? "student";
        return Redirect($"/u/{username}");
    }

    // POST: /Profile/UpdateProfile
    [HttpPost("/Profile/UpdateProfile")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateDeveloperProfileRequest request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Json(new { success = false, message = "Chưa đăng nhập." });
        }

        bool isEn = HttpContext.IsEnglish();
        bool success = await _portfolioService.UpdateProfileAsync(currentUserId, request);

        return Json(new 
        { 
            success, 
            message = isEn ? "Developer portfolio updated successfully!" : "Đã cập nhật hồ sơ năng lực thành công!" 
        });
    }
}
