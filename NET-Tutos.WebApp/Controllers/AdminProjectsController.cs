using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

[Authorize(Roles = "Admin")]
public class AdminProjectsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICapstoneProjectService _projectService;
    private readonly ILogger<AdminProjectsController> _logger;

    public AdminProjectsController(
        AppDbContext context,
        ICapstoneProjectService projectService,
        ILogger<AdminProjectsController> logger)
    {
        _context = context;
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var projects = await _context.CapstoneProjects
            .Include(p => p.Category)
            .Include(p => p.Submissions)
            .OrderBy(p => p.OrderIndex)
            .ToListAsync();

        return View(projects);
    }

    [HttpGet]
    public async Task<IActionResult> Submissions(ProjectSubmissionStatus? status)
    {
        var viewModel = await _projectService.GetAdminSubmissionsAsync(status);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var submission = await _projectService.GetSubmissionByIdAsync(id);
        if (submission == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy bài nộp tương ứng.";
            return RedirectToAction(nameof(Submissions));
        }

        var model = new AdminReviewInputModel
        {
            SubmissionId = submission.Id,
            Score = submission.Score ?? 85,
            Status = submission.Status == ProjectSubmissionStatus.Approved ? ProjectSubmissionStatus.Approved : ProjectSubmissionStatus.Approved,
            ReviewerFeedback = submission.ReviewerFeedback ?? "Mã nguồn tổ chức tốt, tuân thủ Clean Architecture và các nguyên lý SOLID. Cấu trúc thư mục rõ ràng, code sạch và dễ đọc. Đạt yêu cầu đồ án!",
            BonusXp = 50
        };

        ViewBag.Submission = submission;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(AdminReviewInputModel model)
    {
        var submission = await _projectService.GetSubmissionByIdAsync(model.SubmissionId);
        if (submission == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy bài nộp tương ứng.";
            return RedirectToAction(nameof(Submissions));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Submission = submission;
            return View(model);
        }

        string reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, message) = await _projectService.ReviewSubmissionAsync(reviewerId, model);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Submissions));
        }
        else
        {
            TempData["ErrorMessage"] = message;
            ViewBag.Submission = submission;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var project = await _context.CapstoneProjects.FindAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        project.IsPublished = !project.IsPublished;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã {(project.IsPublished ? "công khai" : "ẩn")} đồ án '{project.Title}'.";
        return RedirectToAction(nameof(Index));
    }
}
