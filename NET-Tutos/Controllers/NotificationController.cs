using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLatest(int take = 15)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _notificationService.GetUserNotificationsAsync(userId, take);
        bool isEn = HttpContext.IsEnglish();
        if (isEn)
        {
            foreach (var item in result.Items)
            {
                item.TimeAgo = NotificationService.FormatTimeAgo(item.CreatedAt, true);
                item.TypeName = LocalizationHelper.GetNotificationTypeName(item.Type, true);
            }
        }
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.MarkAsReadAsync(id, userId);
        return Json(new { success });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.MarkAllAsReadAsync(userId);
        return Json(new { success });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.DeleteNotificationAsync(id, userId);
        return Json(new { success });
    }
}
