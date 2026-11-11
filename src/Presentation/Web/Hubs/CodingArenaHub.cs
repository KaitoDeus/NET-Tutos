using Microsoft.AspNetCore.SignalR;
using NET_Tutos.Services;

namespace NET_Tutos.Hubs;

/// <summary>
/// Real-Time SignalR Hub for 1v1 and multiplayer C# algorithmic coding battles.
/// </summary>
public class CodingArenaHub : Hub
{
    private readonly ICodingArenaService _arenaService;

    public CodingArenaHub(ICodingArenaService arenaService)
    {
        _arenaService = arenaService;
    }

    public async Task JoinArenaRoom(string roomCode, string playerName)
    {
        var room = _arenaService.JoinRoom(roomCode, playerName);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
        await Clients.Group(room.RoomCode).SendAsync("ArenaStateUpdated", room);
    }

    public async Task LeaveArenaRoom(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode.Trim().ToUpperInvariant());
    }

    public async Task BroadcastTypingProgress(string roomCode, string playerName, int progressPercent)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return;
        string normalizedCode = roomCode.Trim().ToUpperInvariant();
        await Clients.Group(normalizedCode).SendAsync("PlayerProgressUpdated", new
        {
            roomCode = normalizedCode,
            playerName = string.IsNullOrWhiteSpace(playerName) ? "AnonymousCoder" : playerName.Trim(),
            progressPercent = Math.Clamp(progressPercent, 0, 99),
            timestampUtc = DateTime.UtcNow
        });
    }

    public async Task ResetArenaRoom(string roomCode)
    {
        var room = _arenaService.ResetRoom(roomCode);
        await Clients.Group(room.RoomCode).SendAsync("ArenaStateUpdated", room);
    }
}
