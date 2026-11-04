using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public sealed class ArenaPlayerViewModel
{
    public string PlayerName { get; set; } = string.Empty;
    public int EloRating { get; set; } = 1200;
    public int ProgressPercent { get; set; }
    public bool HasSolved { get; set; }
    public long? ExecutionTimeMs { get; set; }
}

public sealed class ArenaRoomViewModel
{
    public string RoomCode { get; set; } = string.Empty;
    public string TitleVi { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionVi { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Intermediate";
    public string StarterCode { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public int RewardXp { get; set; } = 120;
    public int TimeLimitSeconds { get; set; } = 300;
    public string Status { get; set; } = "Open";
    public string? WinnerName { get; set; }
    public List<ArenaPlayerViewModel> Players { get; set; } = [];
}

public sealed class ArenaSubmitRequest
{
    public string RoomCode { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
}

public sealed class ArenaSubmitResponse
{
    public bool IsSuccess { get; set; }
    public bool IsCorrectOutput { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string ActualOutput { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int AwardedXp { get; set; }
    public int EloDelta { get; set; }
    public int NewEloRating { get; set; }
    public string? WinnerName { get; set; }
}

public interface ICodingArenaService
{
    IReadOnlyList<ArenaRoomViewModel> GetActiveRooms();
    ArenaRoomViewModel? GetRoom(string roomCode);
    ArenaRoomViewModel JoinRoom(string roomCode, string playerName);
    Task<ArenaSubmitResponse> SubmitSolutionAsync(ArenaSubmitRequest request);
}

/// <summary>
/// Real-Time 1v1 Multiplayer Coding Arena Engine managing algorithmic battle rooms,
/// Roslyn C# 14 execution validation, ELO rating adjustments, and XP rewards.
/// </summary>
public sealed class CodingArenaService : ICodingArenaService
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ICodeExecutionService? _directCodeExecutionService;
    private readonly ConcurrentDictionary<string, ArenaRoomViewModel> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _playerElo = new(StringComparer.OrdinalIgnoreCase);

    [ActivatorUtilitiesConstructor]
    public CodingArenaService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        SeedDefaultRooms();
    }

    public CodingArenaService(ICodeExecutionService codeExecutionService)
    {
        _directCodeExecutionService = codeExecutionService;
        SeedDefaultRooms();
    }

    public IReadOnlyList<ArenaRoomViewModel> GetActiveRooms()
    {
        return _rooms.Values
            .OrderBy(r => r.RoomCode)
            .ToList();
    }

    public ArenaRoomViewModel? GetRoom(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return null;
        _rooms.TryGetValue(roomCode.Trim(), out var room);
        return room;
    }

    public ArenaRoomViewModel JoinRoom(string roomCode, string playerName)
    {
        string normalizedCode = string.IsNullOrWhiteSpace(roomCode) ? "ARENA-LINQ-01" : roomCode.Trim().ToUpperInvariant();
        string normalizedPlayer = string.IsNullOrWhiteSpace(playerName) ? "AnonymousCoder" : playerName.Trim();

        var room = _rooms.GetOrAdd(normalizedCode, code => new ArenaRoomViewModel
        {
            RoomCode = code,
            TitleVi = $"Đấu trường C# #{code}",
            TitleEn = $"C# Battle Room #{code}",
            DescriptionVi = "Tính tổng các số chẵn từ 1 đến 10 và in kết quả ra Console.",
            DescriptionEn = "Compute the sum of even numbers from 1 to 10 and print the result to the Console.",
            Difficulty = "Beginner",
            StarterCode = "int sum = Enumerable.Range(1, 10).Where(x => x % 2 == 0).Sum();\nConsole.WriteLine(sum);",
            ExpectedOutput = "30",
            RewardXp = 100
        });

        lock (room)
        {
            int currentElo = _playerElo.GetOrAdd(normalizedPlayer, 1200);
            var existing = room.Players.FirstOrDefault(p => p.PlayerName.Equals(normalizedPlayer, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                room.Players.Add(new ArenaPlayerViewModel
                {
                    PlayerName = normalizedPlayer,
                    EloRating = currentElo,
                    ProgressPercent = 10,
                    HasSolved = false
                });
            }
            if (room.Players.Count >= 2 && room.Status == "Open")
            {
                room.Status = "InProgress";
            }
        }

        return room;
    }

    public async Task<ArenaSubmitResponse> SubmitSolutionAsync(ArenaSubmitRequest request)
    {
        string roomCode = string.IsNullOrWhiteSpace(request.RoomCode) ? "ARENA-LINQ-01" : request.RoomCode.Trim().ToUpperInvariant();
        string playerName = string.IsNullOrWhiteSpace(request.PlayerName) ? "AnonymousCoder" : request.PlayerName.Trim();

        var room = JoinRoom(roomCode, playerName);

        ICodeExecutionService execService;
        IDisposable? scopeToDispose = null;
        if (_scopeFactory != null)
        {
            var scope = _scopeFactory.CreateScope();
            scopeToDispose = scope;
            execService = scope.ServiceProvider.GetRequiredService<ICodeExecutionService>();
        }
        else
        {
            execService = _directCodeExecutionService!;
        }

        CodeExecutionResponse execResult;
        try
        {
            execResult = await execService.ExecuteAsync(request.SourceCode, timeoutMs: 4000);
        }
        finally
        {
            scopeToDispose?.Dispose();
        }

        string actual = (execResult.Output ?? string.Empty).Trim();
        string expected = room.ExpectedOutput.Trim();
        bool isCorrect = execResult.IsSuccess && string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

        int eloDelta = 0;
        int awardedXp = 0;
        int newElo;

        lock (room)
        {
            var player = room.Players.First(p => p.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (isCorrect)
            {
                player.ProgressPercent = 100;
                player.HasSolved = true;
                player.ExecutionTimeMs = execResult.ExecutionTimeMs;

                if (string.IsNullOrEmpty(room.WinnerName))
                {
                    room.WinnerName = playerName;
                    room.Status = "Completed";
                    eloDelta = 25;
                    awardedXp = room.RewardXp;
                }
                else
                {
                    eloDelta = 10;
                    awardedXp = room.RewardXp / 2;
                }
            }
            else
            {
                player.ProgressPercent = Math.Max(player.ProgressPercent, execResult.IsSuccess ? 60 : 30);
                eloDelta = -5;
            }

            newElo = _playerElo.AddOrUpdate(playerName, 1200 + eloDelta, (_, old) => Math.Max(800, old + eloDelta));
            player.EloRating = newElo;
        }

        return new ArenaSubmitResponse
        {
            IsSuccess = execResult.IsSuccess,
            IsCorrectOutput = isCorrect,
            RoomCode = room.RoomCode,
            PlayerName = playerName,
            ActualOutput = actual,
            ExpectedOutput = expected,
            ErrorMessage = execResult.Error,
            ExecutionTimeMs = execResult.ExecutionTimeMs,
            AwardedXp = awardedXp,
            EloDelta = eloDelta,
            NewEloRating = newElo,
            WinnerName = room.WinnerName
        };
    }

    private void SeedDefaultRooms()
    {
        _rooms["ARENA-LINQ-01"] = new ArenaRoomViewModel
        {
            RoomCode = "ARENA-LINQ-01",
            TitleVi = "Đấu trường #1: Truy vấn LINQ Tốc độ cao",
            TitleEn = "Battle #1: High-Speed LINQ Aggregation",
            DescriptionVi = "Cho mảng [3, 8, 12, 5, 19, 24]. Lọc các số chia hết cho 4, nhân đôi giá trị và in ra tổng cuối cùng (88).",
            DescriptionEn = "Given [3, 8, 12, 5, 19, 24], filter numbers divisible by 4, double each value, and print their sum (88).",
            Difficulty = "Beginner",
            StarterCode = "int[] nums = [3, 8, 12, 5, 19, 24];\nint result = nums.Where(n => n % 4 == 0).Select(n => n * 2).Sum();\nConsole.WriteLine(result);",
            ExpectedOutput = "88",
            RewardXp = 100,
            TimeLimitSeconds = 180,
            Status = "Open"
        };

        _rooms["ARENA-ALGO-02"] = new ArenaRoomViewModel
        {
            RoomCode = "ARENA-ALGO-02",
            TitleVi = "Đấu trường #2: Chuỗi Fibonacci Thứ 10",
            TitleEn = "Battle #2: 10th Fibonacci Number O(N)",
            DescriptionVi = "Viết thuật toán vòng lặp O(N) tính và in ra số Fibonacci thứ 10 (F(0)=0, F(1)=1 -> F(10)=55).",
            DescriptionEn = "Write an O(N) iterative algorithm to compute and print the 10th Fibonacci number (F(10)=55).",
            Difficulty = "Intermediate",
            StarterCode = "int a = 0, b = 1;\nfor (int i = 2; i <= 10; i++)\n{\n    (a, b) = (b, a + b);\n}\nConsole.WriteLine(b);",
            ExpectedOutput = "55",
            RewardXp = 150,
            TimeLimitSeconds = 240,
            Status = "Open"
        };

        _rooms["ARENA-CS14-03"] = new ArenaRoomViewModel
        {
            RoomCode = "ARENA-CS14-03",
            TitleVi = "Đấu trường #3: Pattern Matching & Record C# 14",
            TitleEn = "Battle #3: C# 14 Pattern Matching & Records",
            DescriptionVi = "Tính tổng số điểm thưởng của các đơn hàng có giá trị > 100 trong danh sách và in kết quả (450).",
            DescriptionEn = "Compute the total bonus points for orders exceeding 100 and print the result (450).",
            Difficulty = "Advanced",
            StarterCode = "int[] orders = [80, 150, 300, 95];\nint total = orders.Where(x => x > 100).Sum();\nConsole.WriteLine(total);",
            ExpectedOutput = "450",
            RewardXp = 200,
            TimeLimitSeconds = 300,
            Status = "Open"
        };
    }
}
