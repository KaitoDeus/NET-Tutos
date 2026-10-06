using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class InterviewController : Controller
{
    private readonly IFlashcardService _flashcardService;
    private readonly ILogger<InterviewController> _logger;

    public InterviewController(
        IFlashcardService flashcardService,
        ILogger<InterviewController> logger)
    {
        _flashcardService = flashcardService;
        _logger = logger;
    }

    // GET: /Interview or /phong-van-csharp or /interview-prep
    public async Task<IActionResult> Index(
        FlashcardTopic? topic = null,
        DifficultyLevel? difficulty = null,
        string? search = null,
        int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isEn = HttpContext.IsEnglish();

        var viewModel = await _flashcardService.GetIndexDataAsync(
            userId, topic, difficulty, search, page, pageSize: 12, isEn: isEn);

        return View(viewModel);
    }

    // GET: /Interview/Study
    public async Task<IActionResult> Study(
        FlashcardTopic? topic = null,
        DifficultyLevel? difficulty = null,
        int limit = 15)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isEn = HttpContext.IsEnglish();

        var sessionModel = await _flashcardService.GetStudySessionAsync(
            userId, topic, difficulty, limit, isEn);

        if (!sessionModel.QueueCards.Any())
        {
            TempData["SuccessMessage"] = isEn 
                ? "You've reviewed all cards in this topic for today! Great job!"
                : "Bạn đã hoàn thành ôn tập tất cả các thẻ trong chủ đề này hôm nay! Tuyệt vời!";
            return RedirectToAction(nameof(Index));
        }

        return View(sessionModel);
    }

    // POST: /Interview/Review (AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review([FromBody] FlashcardReviewRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập để lưu tiến độ ôn tập và nhận điểm XP." });
        }

        bool isEn = HttpContext.IsEnglish();
        var response = await _flashcardService.RecordReviewAsync(
            userId, request.FlashcardId, request.Rating, isEn);

        return Json(response);
    }

    // GET: /Interview/Mock
    public async Task<IActionResult> Mock(DifficultyLevel? difficulty = null, int count = 5)
    {
        bool isEn = HttpContext.IsEnglish();
        var mockModel = await _flashcardService.CreateMockInterviewAsync(difficulty, count, isEn);

        return View(mockModel);
    }

    // POST: /Interview/SubmitMock (AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitMock([FromBody] SubmitMockInterviewRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isEn = HttpContext.IsEnglish();

        var result = await _flashcardService.SubmitMockInterviewAsync(userId ?? "", request, isEn);
        return Json(result);
    }

    // GET: /Interview/Question/{slug}
    public async Task<IActionResult> QuestionDetail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isEn = HttpContext.IsEnglish();

        var card = await _flashcardService.GetFlashcardBySlugAsync(slug, userId, isEn);
        if (card == null) return NotFound();

        return View(card);
    }
}
