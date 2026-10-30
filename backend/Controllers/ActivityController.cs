using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class ActivityController : Controller
{
    private readonly IActivityFeedService _activityFeedService;

    public ActivityController(IActivityFeedService activityFeedService)
    {
        _activityFeedService = activityFeedService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? filter, int page = 1)
    {
        var model = await _activityFeedService.GetActivitiesPagedAsync(page, pageSize: 15, filter);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent(int take = 10, string? filter = null)
    {
        var model = await _activityFeedService.GetActivitiesPagedAsync(page: 1, pageSize: take, filter);
        return Json(model.Activities);
    }
}
