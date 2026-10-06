using NET_Tutos.Models.Entities;
using NET_Tutos.Services;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class CurriculumController : Controller
{
    private readonly ICurriculumService _curriculumService;

    public CurriculumController(ICurriculumService curriculumService)
    {
        _curriculumService = curriculumService;
    }

    // GET: /chuong-trinh-csharp-toan-dien hoặc /Curriculum
    public async Task<IActionResult> Index(int? section, DifficultyLevel? level, string? search)
    {
        var model = await _curriculumService.GetCurriculumOverviewAsync(section, level, search);
        return View(model);
    }

    // GET: /chuong-trinh-csharp/{id}
    public async Task<IActionResult> Details(int id)
    {
        var model = await _curriculumService.GetLessonDetailAsync(id);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }
}
