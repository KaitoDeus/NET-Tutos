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

    public async Task BroadcastTypingProgress(string roomCode, string playerName, int progressPercent)
    {
        await Clients.Group(roomCode.Trim().ToUpperInvariant()).SendAsync("PlayerProgressUpdated", new
        {
            roomCode = roomCode.Trim().ToUpperInvariant(),
            playerName,
            progressPercent = Math.Clamp(progressPercent, 0, 99),
            timestampUtc = DateTime.UtcNow
        });
    }
}
