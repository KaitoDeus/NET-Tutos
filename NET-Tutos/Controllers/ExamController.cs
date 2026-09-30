using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class ExamController : Controller
{
    private readonly IExamService _examService;

    public ExamController(IExamService examService)
    {
        _examService = examService;
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
