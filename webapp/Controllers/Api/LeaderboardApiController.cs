using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers.Api;

[ApiController]
[Route("api/leaderboard")]
[Produces("application/json")]
public class LeaderboardApiController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly IMobileAuthService _mobileAuthService;

    public LeaderboardApiController(
        ILeaderboardService leaderboardService,
        IMobileAuthService mobileAuthService)
    {
        _leaderboardService = leaderboardService;
        _mobileAuthService = mobileAuthService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 50)
    {
        string? currentUserId = null;
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var (isValid, userId) = _mobileAuthService.ValidateToken(token);
            if (isValid) currentUserId = userId;
        }

        var result = await _leaderboardService.GetLeaderboardAsync(currentUserId, limit);
        return Ok(result);
    }
}
