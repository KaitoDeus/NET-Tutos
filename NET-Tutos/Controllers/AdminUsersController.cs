using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUsersController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /AdminUsers
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.ExperiencePoints)
            .ToListAsync();

        var userItems = new List<UserManagementItem>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var completedLessons = await _context.UserLessonProgresses
                .CountAsync(p => p.UserId == user.Id && p.IsCompleted);
            var certCount = await _context.Certificates
                .CountAsync(c => c.UserId == user.Id);

            userItems.Add(new UserManagementItem
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? user.UserName ?? "Học viên",
                Roles = roles,
                ExperiencePoints = user.ExperiencePoints,
                CompletedLessonsCount = completedLessons,
                CertificatesCount = certCount,
                CreatedAt = user.CreatedAt
            });
        }

        var viewModel = new UserManagementViewModel
        {
            Users = userItems
        };

        return View(viewModel);
    }

    // POST: /AdminUsers/ToggleAdmin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự gỡ quyền Quản trị viên của chính mình!";
            return RedirectToAction(nameof(Index));
        }

        var targetUser = await _userManager.FindByIdAsync(id);
        if (targetUser == null)
        {
            return NotFound();
        }

        var isAdmin = await _userManager.IsInRoleAsync(targetUser, "Admin");
        if (isAdmin)
        {
            await _userManager.RemoveFromRoleAsync(targetUser, "Admin");
            TempData["SuccessMessage"] = $"Đã hạ quyền Quản trị viên của học viên {targetUser.Email}.";
        }
        else
        {
            await _userManager.AddToRoleAsync(targetUser, "Admin");
            TempData["SuccessMessage"] = $"Đã cấp quyền Quản trị viên cho tài khoản {targetUser.Email}.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /AdminUsers/AwardXp
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AwardXp(string id, int xpAmount)
    {
        if (xpAmount <= 0 || xpAmount > 1000)
        {
            TempData["ErrorMessage"] = "Số điểm XP thưởng không hợp lệ (1 - 1000).";
            return RedirectToAction(nameof(Index));
        }

        var targetUser = await _userManager.FindByIdAsync(id);
        if (targetUser == null)
        {
            return NotFound();
        }

        targetUser.ExperiencePoints += xpAmount;
        await _userManager.UpdateAsync(targetUser);

        TempData["SuccessMessage"] = $"Đã thưởng +{xpAmount} XP cho {targetUser.FullName ?? targetUser.Email}!";
        return RedirectToAction(nameof(Index));
    }
}
