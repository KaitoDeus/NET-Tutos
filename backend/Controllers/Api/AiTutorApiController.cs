using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers.Api;

[ApiController]
[Route("api/aitutor")]
[Produces("application/json")]
public class AiTutorApiController : ControllerBase
{
    private readonly IAiTutorService _aiTutorService;

    public AiTutorApiController(IAiTutorService aiTutorService)
    {
        _aiTutorService = aiTutorService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiTutorChatRequest request, [FromHeader(Name = "Accept-Language")] string? language)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập câu hỏi hoặc yêu cầu cho AI Tutor." });
        }

        bool isEnglish = !string.IsNullOrEmpty(language) && language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var response = await _aiTutorService.ChatAsync(request, isEnglish);
        return Ok(response);
    }

    [HttpPost("explain-error")]
    public async Task<IActionResult> ExplainError([FromBody] AiExplainErrorRequest request, [FromHeader(Name = "Accept-Language")] string? language)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ErrorMessage))
        {
            return BadRequest(new { success = false, message = "Không tìm thấy thông tin lỗi để giải thích." });
        }

        bool isEnglish = !string.IsNullOrEmpty(language) && language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var response = await _aiTutorService.ExplainErrorAsync(request, isEnglish);
        return Ok(response);
    }
}
