using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class ExamController : Controller
{
    private readonly IExamService _examService;
    private readonly IStreakService _streakService;
    private readonly INotificationService _notificationService;
    private readonly IActivityFeedService _activityFeedService;

    public ExamController(
        IExamService examService,
        IStreakService streakService,
        INotificationService notificationService,
        IActivityFeedService activityFeedService)
    {
        _examService = examService;
        _streakService = streakService;
        _notificationService = notificationService;
        _activityFeedService = activityFeedService;
    }

    // GET: /Exam
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var viewModel = await _examService.GetExamListAsync(userId);
        return View(viewModel);
    }

    // GET: /Exam/Take/{categoryId?}
    [Authorize]
    public async Task<IActionResult> Take(int? categoryId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var session = await _examService.StartExamSessionAsync(userId, categoryId);

        if (session == null || !session.Questions.Any())
        {
            TempData["ErrorMessage"] = "Chưa có đủ câu hỏi để tổ chức kỳ thi này.";
            return RedirectToAction(nameof(Index));
        }

        return View(session);
    }

    // POST: /Exam/Submit
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Submit([FromBody] ExamSubmissionViewModel model)
    {
        if (model == null || !model.Answers.Any())
        {
            return BadRequest(new { success = false, message = "Dữ liệu bài nộp không hợp lệ." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var hostUrl = $"{Request.Scheme}://{Request.Host}";

        var result = await _examService.EvaluateExamAsync(userId, model, hostUrl);

        if (result.IsPassed)
        {
            await _streakService.RecordLearningActivityStreakAsync(userId);

            try
            {
                var examUrl = Url.Action(nameof(Result), new { id = result.AttemptId });
                await _notificationService.CreateNotificationAsync(
                    userId,
                    "Chúc mừng bạn đã thi đỗ kỳ thi! 🎓",
                    $"Bạn đã hoàn thành kỳ thi với kết quả {result.ScorePercentage}% (+50 XP)!",
                    NotificationType.ExamPassed,
                    examUrl);

                await _activityFeedService.RecordActivityAsync(
                    userId,
                    ActivityType.ExamPassed,
                    "đã vượt qua kỳ thi đánh giá",
                    $"Đạt kết quả {result.ScorePercentage}% chuẩn chuyên môn",
                    examUrl,
                    xpEarned: 50);

                if (result.Certificate != null)
                {
                    var certUrl = Url.Action("ViewCertificate", "Certificate", new { code = result.Certificate.CertificateCode });
                    await _notificationService.CreateNotificationAsync(
                        userId,
                        "Chứng chỉ số đã sẵn sàng! 📜",
                        $"Chúc mừng bạn nhận được Chứng chỉ số #{result.Certificate.CertificateCode}.",
                        NotificationType.CertificateIssued,
                        certUrl);

                    await _activityFeedService.RecordActivityAsync(
                        userId,
                        ActivityType.CertificateEarned,
                        "đã được cấp Chứng chỉ số chính thức",
                        $"Mã chứng chỉ #{result.Certificate.CertificateCode} 🎓",
                        certUrl,
                        xpEarned: 100);
                }
            }
            catch { }
        }

        return Ok(new
        {
            success = true,
            attemptId = result.AttemptId,
            score = result.ScorePercentage,
            isPassed = result.IsPassed,
            correctCount = result.CorrectAnswers,
            totalCount = result.TotalQuestions,
            certificateCode = result.Certificate?.CertificateCode,
            redirectUrl = Url.Action(nameof(Result), new { id = result.AttemptId })
        });
    }

    // GET: /Exam/Result/{id}
    [Authorize]
    public async Task<IActionResult> Result(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _examService.GetExamAttemptResultAsync(id, userId);

        if (result == null)
        {
            return NotFound();
        }

        return View(result);
    }
}
