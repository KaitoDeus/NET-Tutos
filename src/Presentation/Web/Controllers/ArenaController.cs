using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NET_Tutos.Hubs;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class ArenaController : Controller
{
    private readonly ICodingArenaService _arenaService;
    private readonly IHubContext<CodingArenaHub> _hubContext;

    public ArenaController(ICodingArenaService arenaService, IHubContext<CodingArenaHub> hubContext)
    {
        _arenaService = arenaService;
        _hubContext = hubContext;
    }

    [HttpGet]
    public IActionResult Index(string? room)
    {
        var rooms = _arenaService.GetActiveRooms();
        ViewBag.SelectedRoomCode = string.IsNullOrWhiteSpace(room)
            ? (rooms.FirstOrDefault()?.RoomCode ?? "ARENA-LINQ-01")
            : room.Trim().ToUpperInvariant();
        ViewBag.ArenaLeaderboard = _arenaService.GetLeaderboard(10);
        ViewBag.RecentMatches = _arenaService.GetRecentMatches(null, 12);
        return View(rooms);
    }

    [HttpGet("/api/arena/rooms")]
    public IActionResult GetRooms()
    {
        return Ok(_arenaService.GetActiveRooms());
    }

    [HttpGet("/api/arena/leaderboard")]
    public IActionResult GetLeaderboard([FromQuery] int top = 10)
    {
        return Ok(_arenaService.GetLeaderboard(top));
    }

    [HttpGet("/api/arena/matches")]
    public IActionResult GetMatches([FromQuery] string? roomCode = null, [FromQuery] int limit = 15)
    {
        return Ok(_arenaService.GetRecentMatches(roomCode, limit));
    }

    [HttpPost("/api/arena/join")]
    public async Task<IActionResult> Join([FromBody] ArenaJoinDto dto)
    {
        var room = _arenaService.JoinRoom(dto.RoomCode, dto.PlayerName);
        await _hubContext.Clients.Group(room.RoomCode).SendAsync("ArenaStateUpdated", room);
        return Ok(room);
    }

    [HttpPost("/api/arena/reset")]
    public async Task<IActionResult> Reset([FromBody] ArenaResetDto dto)
    {
        var room = _arenaService.ResetRoom(dto.RoomCode);
        await _hubContext.Clients.Group(room.RoomCode).SendAsync("ArenaStateUpdated", room);
        return Ok(room);
    }

    [HttpPost("/api/arena/submit")]
    public async Task<IActionResult> Submit([FromBody] ArenaSubmitRequest request)
    {
        var result = await _arenaService.SubmitSolutionAsync(request);
        var updatedRoom = _arenaService.GetRoom(result.RoomCode);
        if (updatedRoom != null)
        {
            await _hubContext.Clients.Group(result.RoomCode).SendAsync("ArenaStateUpdated", updatedRoom);
            await _hubContext.Clients.Group(result.RoomCode).SendAsync("ArenaLeaderboardUpdated", new
            {
                leaderboard = _arenaService.GetLeaderboard(10),
                matches = _arenaService.GetRecentMatches(null, 12)
            });
        }
        return Ok(result);
    }
}

public sealed class ArenaJoinDto
{
    public string RoomCode { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}

public sealed class ArenaResetDto
{
    public string RoomCode { get; set; } = string.Empty;
}
