using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
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
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public List<ArenaPlayerViewModel> Players { get; set; } = [];
}

public sealed class ArenaLeaderboardEntryViewModel
{
    public string PlayerName { get; set; } = string.Empty;
    public int EloRating { get; set; } = 1200;
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public long? BestExecutionTimeMs { get; set; }
    public int TotalXpEarned { get; set; }
}

public sealed class ArenaMatchHistoryViewModel
{
    public string RoomCode { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public bool IsCorrectOutput { get; set; }
    public bool IsWinner { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int EloDelta { get; set; }
    public int NewEloRating { get; set; }
    public int AwardedXp { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
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
    ArenaRoomViewModel ResetRoom(string roomCode);
    IReadOnlyList<ArenaLeaderboardEntryViewModel> GetLeaderboard(int top = 10);
    IReadOnlyList<ArenaMatchHistoryViewModel> GetRecentMatches(string? roomCode = null, int limit = 15);
    Task<ArenaSubmitResponse> SubmitSolutionAsync(ArenaSubmitRequest request);
}

/// <summary>
/// Real-Time 1v1 Multiplayer Coding Arena Engine managing PostgreSQL-persistent battle rooms,
/// Roslyn C# 14 execution validation, ELO rating persistence, match history, and XP rewards.
/// </summary>
public sealed class CodingArenaService : ICodingArenaService
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ICodeExecutionService? _directCodeExecutionService;
    private readonly ConcurrentDictionary<string, ArenaRoomViewModel> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _playerElo = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ArenaMatchHistoryViewModel> _inMemoryHistory = new();
    private readonly object _hydrateLock = new();
    private bool _dbHydrated;

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
        EnsureDbHydrated();
        return _rooms.Values
            .OrderBy(r => r.RoomCode)
            .ToList();
    }

    public ArenaRoomViewModel? GetRoom(string roomCode)
    {
        EnsureDbHydrated();
        if (string.IsNullOrWhiteSpace(roomCode)) return null;
        _rooms.TryGetValue(roomCode.Trim(), out var room);
        return room;
    }

    public ArenaRoomViewModel JoinRoom(string roomCode, string playerName)
    {
        EnsureDbHydrated();
        string normalizedCode = NormalizeRoomCode(roomCode);
        string normalizedPlayer = NormalizePlayerName(playerName);

        var room = GetOrCreateRoomModel(normalizedCode);

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

            if (room.StartedAtUtc == null && room.Status != "Completed")
            {
                room.StartedAtUtc = DateTime.UtcNow;
            }
            if (room.Players.Count >= 2 && room.Status == "Open")
            {
                room.Status = "InProgress";
            }
        }

        PersistRoomAndPlayerSnapshot(room, normalizedPlayer, _playerElo.GetValueOrDefault(normalizedPlayer, 1200));
        return room;
    }

    public ArenaRoomViewModel ResetRoom(string roomCode)
    {
        EnsureDbHydrated();
        string normalizedCode = NormalizeRoomCode(roomCode);
        var room = GetOrCreateRoomModel(normalizedCode);

        lock (room)
        {
            room.Status = "Open";
            room.WinnerName = null;
            room.StartedAtUtc = null;
            room.CompletedAtUtc = null;
            room.Players.Clear();
        }

        PersistRoomAndPlayerSnapshot(room, null, null);
        return room;
    }

    public IReadOnlyList<ArenaLeaderboardEntryViewModel> GetLeaderboard(int top = 10)
    {
        EnsureDbHydrated();
        int clampedTop = Math.Clamp(top, 1, 50);

        if (TryWithDbContext(db =>
        {
            if (db.ArenaPlayerRatings.Any())
            {
                return db.ArenaPlayerRatings
                    .AsNoTracking()
                    .OrderByDescending(p => p.EloRating)
                    .ThenByDescending(p => p.Wins)
                    .ThenBy(p => p.BestExecutionTimeMs ?? long.MaxValue)
                    .Take(clampedTop)
                    .Select(p => new ArenaLeaderboardEntryViewModel
                    {
                        PlayerName = p.PlayerName,
                        EloRating = p.EloRating,
                        MatchesPlayed = p.MatchesPlayed,
                        Wins = p.Wins,
                        Losses = p.Losses,
                        BestExecutionTimeMs = p.BestExecutionTimeMs,
                        TotalXpEarned = p.TotalXpEarned
                    })
                    .ToList();
            }
            return null;
        }, out var dbLeaderboard) && dbLeaderboard != null)
        {
            return dbLeaderboard;
        }

        lock (_inMemoryHistory)
        {
            return _playerElo
                .Select(kvp =>
                {
                    var playerMatches = _inMemoryHistory.Where(m => m.PlayerName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)).ToList();
                    return new ArenaLeaderboardEntryViewModel
                    {
                        PlayerName = kvp.Key,
                        EloRating = kvp.Value,
                        MatchesPlayed = playerMatches.Count,
                        Wins = playerMatches.Count(m => m.IsWinner),
                        Losses = playerMatches.Count(m => !m.IsCorrectOutput),
                        BestExecutionTimeMs = playerMatches.Where(m => m.IsCorrectOutput).Select(m => (long?)m.ExecutionTimeMs).Min(),
                        TotalXpEarned = playerMatches.Sum(m => m.AwardedXp)
                    };
                })
                .OrderByDescending(e => e.EloRating)
                .ThenByDescending(e => e.Wins)
                .Take(clampedTop)
                .ToList();
        }
    }

    public IReadOnlyList<ArenaMatchHistoryViewModel> GetRecentMatches(string? roomCode = null, int limit = 15)
    {
        EnsureDbHydrated();
        int clampedLimit = Math.Clamp(limit, 1, 50);
        string? normalizedCode = string.IsNullOrWhiteSpace(roomCode) ? null : roomCode.Trim().ToUpperInvariant();

        if (TryWithDbContext(db =>
        {
            var query = db.ArenaMatchRecords.AsNoTracking();
            if (!string.IsNullOrEmpty(normalizedCode))
            {
                query = query.Where(m => m.RoomCode == normalizedCode);
            }
            return query
                .OrderByDescending(m => m.SubmittedAtUtc)
                .Take(clampedLimit)
                .Select(m => new ArenaMatchHistoryViewModel
                {
                    RoomCode = m.RoomCode,
                    PlayerName = m.PlayerName,
                    IsCorrectOutput = m.IsCorrectOutput,
                    IsWinner = m.IsWinner,
                    ExecutionTimeMs = m.ExecutionTimeMs,
                    EloDelta = m.EloDelta,
                    NewEloRating = m.NewEloRating,
                    AwardedXp = m.AwardedXp,
                    SubmittedAtUtc = m.SubmittedAtUtc
                })
                .ToList();
        }, out var dbMatches) && dbMatches != null)
        {
            return dbMatches;
        }

        lock (_inMemoryHistory)
        {
            IEnumerable<ArenaMatchHistoryViewModel> query = _inMemoryHistory;
            if (!string.IsNullOrEmpty(normalizedCode))
            {
                query = query.Where(m => m.RoomCode.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase));
            }
            return query.OrderByDescending(m => m.SubmittedAtUtc).Take(clampedLimit).ToList();
        }
    }

    public async Task<ArenaSubmitResponse> SubmitSolutionAsync(ArenaSubmitRequest request)
    {
        EnsureDbHydrated();
        string roomCode = NormalizeRoomCode(request.RoomCode);
        string playerName = NormalizePlayerName(request.PlayerName);

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

        int eloDelta;
        int awardedXp = 0;
        int newElo;
        bool isWinner = false;

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
                    room.CompletedAtUtc = DateTime.UtcNow;
                    isWinner = true;
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

        var historyRecord = new ArenaMatchHistoryViewModel
        {
            RoomCode = room.RoomCode,
            PlayerName = playerName,
            IsCorrectOutput = isCorrect,
            IsWinner = isWinner,
            ExecutionTimeMs = execResult.ExecutionTimeMs,
            EloDelta = eloDelta,
            NewEloRating = newElo,
            AwardedXp = awardedXp,
            SubmittedAtUtc = DateTime.UtcNow
        };

        lock (_inMemoryHistory)
        {
            _inMemoryHistory.Add(historyRecord);
            if (_inMemoryHistory.Count > 200)
            {
                _inMemoryHistory.RemoveAt(0);
            }
        }

        PersistSubmissionResult(room, historyRecord, execResult.IsSuccess);

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

    private ArenaRoomViewModel GetOrCreateRoomModel(string normalizedCode)
    {
        return _rooms.GetOrAdd(normalizedCode, code => new ArenaRoomViewModel
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
    }

    private static string NormalizeRoomCode(string? roomCode)
        => string.IsNullOrWhiteSpace(roomCode) ? "ARENA-LINQ-01" : roomCode.Trim().ToUpperInvariant();

    private static string NormalizePlayerName(string? playerName)
        => string.IsNullOrWhiteSpace(playerName) ? "AnonymousCoder" : playerName.Trim();

    private void EnsureDbHydrated()
    {
        if (_dbHydrated || _scopeFactory == null) return;

        lock (_hydrateLock)
        {
            if (_dbHydrated) return;

            TryWithDbContext<bool>(db =>
            {
                // Hydrate persisted player ELO scores
                foreach (var rating in db.ArenaPlayerRatings.AsNoTracking().ToList())
                {
                    _playerElo[rating.PlayerName] = rating.EloRating;
                }

                // Hydrate persisted room states or seed default rooms into DB
                var dbRooms = db.ArenaRooms.ToList();
                var existingCodes = dbRooms.Select(r => r.RoomCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

                bool addedRooms = false;
                foreach (var seed in _rooms.Values)
                {
                    if (!existingCodes.Contains(seed.RoomCode))
                    {
                        db.ArenaRooms.Add(new ArenaRoom
                        {
                            RoomCode = seed.RoomCode,
                            TitleVi = seed.TitleVi,
                            TitleEn = seed.TitleEn,
                            DescriptionVi = seed.DescriptionVi,
                            DescriptionEn = seed.DescriptionEn,
                            Difficulty = seed.Difficulty,
                            StarterCode = seed.StarterCode,
                            ExpectedOutput = seed.ExpectedOutput,
                            RewardXp = seed.RewardXp,
                            TimeLimitSeconds = seed.TimeLimitSeconds,
                            Status = seed.Status,
                            WinnerName = seed.WinnerName,
                            StartedAtUtc = seed.StartedAtUtc,
                            CompletedAtUtc = seed.CompletedAtUtc,
                            CreatedAtUtc = DateTime.UtcNow,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                        addedRooms = true;
                    }
                }

                if (addedRooms)
                {
                    db.SaveChanges();
                    dbRooms = db.ArenaRooms.AsNoTracking().ToList();
                }

                foreach (var persisted in dbRooms)
                {
                    if (_rooms.TryGetValue(persisted.RoomCode, out var memRoom))
                    {
                        memRoom.Status = persisted.Status;
                        memRoom.WinnerName = persisted.WinnerName;
                        memRoom.StartedAtUtc = persisted.StartedAtUtc;
                        memRoom.CompletedAtUtc = persisted.CompletedAtUtc;
                    }
                    else
                    {
                        _rooms[persisted.RoomCode] = new ArenaRoomViewModel
                        {
                            RoomCode = persisted.RoomCode,
                            TitleVi = persisted.TitleVi,
                            TitleEn = persisted.TitleEn,
                            DescriptionVi = persisted.DescriptionVi,
                            DescriptionEn = persisted.DescriptionEn,
                            Difficulty = persisted.Difficulty,
                            StarterCode = persisted.StarterCode,
                            ExpectedOutput = persisted.ExpectedOutput,
                            RewardXp = persisted.RewardXp,
                            TimeLimitSeconds = persisted.TimeLimitSeconds,
                            Status = persisted.Status,
                            WinnerName = persisted.WinnerName,
                            StartedAtUtc = persisted.StartedAtUtc,
                            CompletedAtUtc = persisted.CompletedAtUtc
                        };
                    }
                }

                return true;
            }, out _);

            _dbHydrated = true;
        }
    }

    private void PersistRoomAndPlayerSnapshot(ArenaRoomViewModel room, string? playerName, int? currentElo)
    {
        TryWithDbContext<bool>(db =>
        {
            var dbRoom = EnsureDbRoomRow(db, room);
            dbRoom.Status = room.Status;
            dbRoom.WinnerName = room.WinnerName;
            dbRoom.StartedAtUtc = room.StartedAtUtc;
            dbRoom.CompletedAtUtc = room.CompletedAtUtc;
            dbRoom.UpdatedAtUtc = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(playerName) && currentElo.HasValue)
            {
                var dbPlayer = db.ArenaPlayerRatings.FirstOrDefault(p => p.PlayerName == playerName);
                if (dbPlayer == null)
                {
                    db.ArenaPlayerRatings.Add(new ArenaPlayerRating
                    {
                        PlayerName = playerName,
                        EloRating = currentElo.Value,
                        UpdatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            db.SaveChanges();
            return true;
        }, out _);
    }

    private void PersistSubmissionResult(ArenaRoomViewModel room, ArenaMatchHistoryViewModel match, bool isExecSuccess)
    {
        TryWithDbContext<bool>(db =>
        {
            var dbRoom = EnsureDbRoomRow(db, room);
            dbRoom.Status = room.Status;
            dbRoom.WinnerName = room.WinnerName;
            dbRoom.StartedAtUtc = room.StartedAtUtc;
            dbRoom.CompletedAtUtc = room.CompletedAtUtc;
            dbRoom.UpdatedAtUtc = DateTime.UtcNow;

            var dbPlayer = db.ArenaPlayerRatings.FirstOrDefault(p => p.PlayerName == match.PlayerName);
            if (dbPlayer == null)
            {
                dbPlayer = new ArenaPlayerRating
                {
                    PlayerName = match.PlayerName,
                    EloRating = match.NewEloRating,
                    MatchesPlayed = 1,
                    Wins = match.IsWinner ? 1 : 0,
                    Losses = !match.IsCorrectOutput ? 1 : 0,
                    BestExecutionTimeMs = match.IsCorrectOutput ? match.ExecutionTimeMs : null,
                    TotalXpEarned = match.AwardedXp,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.ArenaPlayerRatings.Add(dbPlayer);
            }
            else
            {
                dbPlayer.EloRating = match.NewEloRating;
                dbPlayer.MatchesPlayed += 1;
                if (match.IsWinner) dbPlayer.Wins += 1;
                if (!match.IsCorrectOutput) dbPlayer.Losses += 1;
                if (match.IsCorrectOutput && (dbPlayer.BestExecutionTimeMs == null || match.ExecutionTimeMs < dbPlayer.BestExecutionTimeMs.Value))
                {
                    dbPlayer.BestExecutionTimeMs = match.ExecutionTimeMs;
                }
                dbPlayer.TotalXpEarned += match.AwardedXp;
                dbPlayer.UpdatedAtUtc = DateTime.UtcNow;
            }

            db.ArenaMatchRecords.Add(new ArenaMatchRecord
            {
                ArenaRoom = dbRoom,
                RoomCode = match.RoomCode,
                PlayerName = match.PlayerName,
                IsSuccess = isExecSuccess,
                IsCorrectOutput = match.IsCorrectOutput,
                IsWinner = match.IsWinner,
                ExecutionTimeMs = match.ExecutionTimeMs,
                EloDelta = match.EloDelta,
                NewEloRating = match.NewEloRating,
                AwardedXp = match.AwardedXp,
                SubmittedAtUtc = match.SubmittedAtUtc
            });

            db.SaveChanges();
            return true;
        }, out _);
    }

    private static ArenaRoom EnsureDbRoomRow(AppDbContext db, ArenaRoomViewModel room)
    {
        var existing = db.ArenaRooms.FirstOrDefault(r => r.RoomCode == room.RoomCode);
        if (existing != null) return existing;

        var created = new ArenaRoom
        {
            RoomCode = room.RoomCode,
            TitleVi = room.TitleVi,
            TitleEn = room.TitleEn,
            DescriptionVi = room.DescriptionVi,
            DescriptionEn = room.DescriptionEn,
            Difficulty = room.Difficulty,
            StarterCode = room.StarterCode,
            ExpectedOutput = room.ExpectedOutput,
            RewardXp = room.RewardXp,
            TimeLimitSeconds = room.TimeLimitSeconds,
            Status = room.Status,
            WinnerName = room.WinnerName,
            StartedAtUtc = room.StartedAtUtc,
            CompletedAtUtc = room.CompletedAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.ArenaRooms.Add(created);
        return created;
    }

    private bool TryWithDbContext<TResult>(Func<AppDbContext, TResult?> operation, out TResult? result)
    {
        result = default;
        if (_scopeFactory == null) return false;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<AppDbContext>();
            if (db == null) return false;

            result = operation(db);
            return true;
        }
        catch
        {
            return false;
        }
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
