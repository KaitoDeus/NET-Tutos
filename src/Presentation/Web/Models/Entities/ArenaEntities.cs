namespace NET_Tutos.Models.Entities;

/// <summary>
/// Persistent coding battle room stored in PostgreSQL / SQLite for 1v1 and multiplayer matches.
/// </summary>
public class ArenaRoom
{
    public int Id { get; set; }
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
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ArenaMatchRecord> Matches { get; set; } = new List<ArenaMatchRecord>();
}

/// <summary>
/// Persistent ELO rating and arena statistics for a coding arena contender.
/// </summary>
public class ArenaPlayerRating
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int EloRating { get; set; } = 1200;
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public long? BestExecutionTimeMs { get; set; }
    public int TotalXpEarned { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Immutable historical audit record for an Arena submission attempt.
/// </summary>
public class ArenaMatchRecord
{
    public long Id { get; set; }
    public int ArenaRoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsCorrectOutput { get; set; }
    public bool IsWinner { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int EloDelta { get; set; }
    public int NewEloRating { get; set; }
    public int AwardedXp { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    public ArenaRoom? ArenaRoom { get; set; }
}
