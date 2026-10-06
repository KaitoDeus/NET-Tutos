using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class AiTutorController : Controller
{
    private readonly IAiTutorService _aiTutorService;

    public AiTutorController(IAiTutorService aiTutorService)
    {
        _aiTutorService = aiTutorService;
    }

    // GET: /AiTutor
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // POST: /AiTutor/Chat
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] AiTutorChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập câu hỏi hoặc yêu cầu cho AI Tutor." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.ChatAsync(request, isEnglish);
        return Json(response);
    }

    // POST: /AiTutor/ExplainError
    [HttpPost]
    public async Task<IActionResult> ExplainError([FromBody] AiExplainErrorRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ErrorMessage))
        {
            return BadRequest(new { success = false, message = "Không tìm thấy thông tin lỗi để giải thích." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.ExplainErrorAsync(request, isEnglish);
        return Json(response);
    }

    // POST: /AiTutor/GetHint
    [HttpPost]
    public async Task<IActionResult> GetHint([FromBody] AiChallengeHintRequest request)
    {
        if (request == null || request.ChallengeId <= 0)
        {
            return BadRequest(new { success = false, message = "Thử thách lập trình không hợp lệ." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.GetChallengeHintAsync(request, isEnglish);
        return Json(response);
    }

    // POST: /AiTutor/ReviewCode
    [HttpPost]
    public async Task<IActionResult> ReviewCode([FromBody] AiCodeReviewRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { success = false, message = "Mã nguồn không được để trống." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.ReviewCodeAsync(request, isEnglish);
        return Json(response);
    }
}
