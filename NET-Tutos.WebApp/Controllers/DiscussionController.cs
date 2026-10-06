using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NET_Tutos.Hubs;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DiscussionController : ControllerBase
{
    private readonly IDiscussionService _discussionService;
    private readonly IHubContext<DiscussionHub> _hubContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public DiscussionController(
        IDiscussionService discussionService,
        IHubContext<DiscussionHub> hubContext,
        UserManager<ApplicationUser> userManager)
    {
        _discussionService = discussionService;
        _hubContext = hubContext;
        _userManager = userManager;
    }

    [HttpGet("comments")]
    public async Task<IActionResult> GetComments([FromQuery] string topicType, [FromQuery] int topicId)
    {
        var currentUserId = _userManager.GetUserId(User);
        var comments = await _discussionService.GetCommentsAsync(topicType, topicId, currentUserId);
        return Ok(comments);
    }

    [HttpPost("comment")]
    [Authorize]
    public async Task<IActionResult> PostComment([FromBody] PostCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Nội dung bình luận không được để trống." });
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var commentDto = await _discussionService.AddCommentAsync(
                userId,
                request.TopicType,
                request.TopicId,
                request.Content,
                request.ParentCommentId);

            var topicGroup = $"{request.TopicType}_{request.TopicId}";

            // Broadcast real-time to everyone in topic group
            await _hubContext.Clients.Group(topicGroup).SendAsync("ReceiveComment", commentDto);

            return Ok(new
            {
                success = true,
                message = "Đã gửi bình luận (+5 XP)!",
                comment = commentDto
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("upvote")]
    [Authorize]
    public async Task<IActionResult> ToggleUpvote([FromBody] ToggleUpvoteRequest request, [FromQuery] string topicType, [FromQuery] int topicId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var (success, count, isUpvoted) = await _discussionService.ToggleUpvoteAsync(request.CommentId, userId);
        if (!success)
        {
            return NotFound(new { message = "Không tìm thấy bình luận." });
        }

        if (!string.IsNullOrEmpty(topicType) && topicId > 0)
        {
            var topicGroup = $"{topicType}_{topicId}";
            await _hubContext.Clients.Group(topicGroup).SendAsync("CommentUpvoted", new
            {
                commentId = request.CommentId,
                upvotesCount = count
            });
        }

        return Ok(new { success = true, upvotesCount = count, isUpvoted });
    }

    [HttpPost("mark-best-answer")]
    [Authorize]
    public async Task<IActionResult> MarkBestAnswer([FromBody] MarkBestAnswerRequest request, [FromQuery] string topicType, [FromQuery] int topicId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        bool isAdmin = User.IsInRole("Admin");
        var (success, message, awardedXp) = await _discussionService.MarkBestAnswerAsync(request.CommentId, userId, isAdmin);

        if (!success)
        {
            return BadRequest(new { message });
        }

        if (!string.IsNullOrEmpty(topicType) && topicId > 0)
        {
            var topicGroup = $"{topicType}_{topicId}";
            await _hubContext.Clients.Group(topicGroup).SendAsync("BestAnswerMarked", new
            {
                commentId = request.CommentId,
                message
            });
        }

        return Ok(new { success = true, message, awardedXp });
    }

    [HttpPost("delete-comment")]
    [Authorize]
    public async Task<IActionResult> DeleteComment([FromBody] MarkBestAnswerRequest request, [FromQuery] string topicType, [FromQuery] int topicId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        bool isAdmin = User.IsInRole("Admin");
        var (success, message) = await _discussionService.DeleteCommentAsync(request.CommentId, userId, isAdmin);

        if (!success)
        {
            return BadRequest(new { message });
        }

        if (!string.IsNullOrEmpty(topicType) && topicId > 0)
        {
            var topicGroup = $"{topicType}_{topicId}";
            await _hubContext.Clients.Group(topicGroup).SendAsync("CommentDeleted", new
            {
                commentId = request.CommentId
            });
        }

        return Ok(new { success = true, message });
    }
}
