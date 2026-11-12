using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace NET_Tutos.Services;

public sealed record SqlTableSchemaDto(string TableName, IReadOnlyList<string> Columns);

public sealed record SqlChallengeDto(
    int Id,
    string Slug,
    string TitleVi,
    string TitleEn,
    string Difficulty,
    string PromptVi,
    string PromptEn,
    string HintVi,
    string HintEn,
    string StarterQuery,
    string ExpectedQuery);

public sealed record SqlExecutionRequest(string Query, int? ChallengeId = null);

public sealed class SqlExecutionResultDto
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public int RowCount { get; set; }
    public bool Truncated { get; set; }
    public long ExecutionTimeMs { get; set; }
    public bool? ChallengePassed { get; set; }
    public string? ChallengeMessage { get; set; }
}

public interface ISqlPlaygroundService
{
    IReadOnlyList<SqlTableSchemaDto> GetSchemaOverview();
    IReadOnlyList<SqlChallengeDto> GetChallenges();
    Task<SqlExecutionResultDto> ExecuteQueryAsync(
        string query,
        int? challengeId = null,
        bool isEnglish = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only in-memory SQLite relational playground for practicing SQL queries and challenges safely.
/// Each execution runs inside an ephemeral in-memory database that is disposed immediately on completion.
/// </summary>
public sealed partial class SqlPlaygroundService : ISqlPlaygroundService
{
    public const int MaxQueryLength = 4_096;
    public const int MaxResultRows = 100;

    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "REPLACE",
        "TRUNCATE", "ATTACH", "DETACH", "PRAGMA", "VACUUM", "REINDEX", "LOAD_EXTENSION"
    ];

    private static readonly IReadOnlyList<SqlTableSchemaDto> SchemaTables =
    [
        new("Students", ["Id (INTEGER)", "FullName (TEXT)", "Country (TEXT)", "XpPoints (INTEGER)", "StreakDays (INTEGER)", "JoinedAt (TEXT)"]),
        new("Tracks", ["Id (INTEGER)", "Slug (TEXT)", "Title (TEXT)", "Difficulty (TEXT)"]),
        new("Enrollments", ["Id (INTEGER)", "StudentId (INTEGER)", "TrackId (INTEGER)", "CompletionPercent (INTEGER)", "EnrolledAt (TEXT)"]),
        new("Submissions", ["Id (INTEGER)", "StudentId (INTEGER)", "ChallengeTitle (TEXT)", "ExecutionMs (INTEGER)", "IsPassed (INTEGER)", "SubmittedAt (TEXT)"])
    ];

    private static readonly IReadOnlyList<SqlChallengeDto> Challenges =
    [
        new(
            Id: 1,
            Slug: "top-xp-students",
            TitleVi: "Top 3 học viên có điểm XP cao nhất",
            TitleEn: "Top 3 Students by XP Points",
            Difficulty: "Beginner",
            PromptVi: "Truy vấn `FullName`, `Country`, `XpPoints` của 3 học viên có `XpPoints` cao nhất, sắp xếp giảm dần.",
            PromptEn: "Select `FullName`, `Country`, and `XpPoints` for the top 3 learners ordered by `XpPoints` descending.",
            HintVi: "Dùng `SELECT ... FROM Students ORDER BY XpPoints DESC LIMIT 3`.",
            HintEn: "Use `SELECT ... FROM Students ORDER BY XpPoints DESC LIMIT 3`.",
            StarterQuery: "SELECT FullName, Country, XpPoints\nFROM Students\nORDER BY XpPoints DESC\nLIMIT 3;",
            ExpectedQuery: "SELECT FullName, Country, XpPoints FROM Students ORDER BY XpPoints DESC LIMIT 3"),
        new(
            Id: 2,
            Slug: "track-completion-avg",
            TitleVi: "Thống kê tiến độ trung bình theo Lộ trình",
            TitleEn: "Average Completion Rate per Track",
            Difficulty: "Intermediate",
            PromptVi: "Lấy `Title`, số lượt đăng ký (`EnrollmentCount`) và tiến độ trung bình làm tròn 1 chữ số thập phân (`AvgProgress`) cho mỗi lộ trình.",
            PromptEn: "Return `Title`, `EnrollmentCount`, and `AvgProgress` (rounded to 1 decimal place) grouped by learning track.",
            HintVi: "Kết hợp `INNER JOIN Enrollments` với `Tracks`, `GROUP BY t.Id, t.Title` và sắp xếp theo `AvgProgress DESC`.",
            HintEn: "Join `Tracks` with `Enrollments`, group by `t.Id, t.Title`, and order by `AvgProgress DESC`.",
            StarterQuery: "SELECT t.Title, COUNT(e.Id) AS EnrollmentCount, ROUND(AVG(e.CompletionPercent), 1) AS AvgProgress\nFROM Tracks t\nINNER JOIN Enrollments e ON t.Id = e.TrackId\nGROUP BY t.Id, t.Title\nORDER BY AvgProgress DESC;",
            ExpectedQuery: "SELECT t.Title, COUNT(e.Id) AS EnrollmentCount, ROUND(AVG(e.CompletionPercent), 1) AS AvgProgress FROM Tracks t INNER JOIN Enrollments e ON t.Id = e.TrackId GROUP BY t.Id, t.Title ORDER BY AvgProgress DESC"),
        new(
            Id: 3,
            Slug: "fast-passing-submissions",
            TitleVi: "Bài nộp C# đạt chuẩn nhanh dưới 50ms",
            TitleEn: "Fast Passing C# Submissions Under 50ms",
            Difficulty: "Beginner",
            PromptVi: "Tìm `FullName`, `ChallengeTitle`, `ExecutionMs` của các bài nộp đạt (`IsPassed = 1`) có thời gian chạy `< 50`, sắp xếp tăng dần theo `ExecutionMs`.",
            PromptEn: "Find `FullName`, `ChallengeTitle`, and `ExecutionMs` for passing submissions (`IsPassed = 1`) faster than 50ms ordered by `ExecutionMs` ascending.",
            HintVi: "Join `Submissions` với `Students`, lọc `WHERE s.IsPassed = 1 AND s.ExecutionMs < 50`.",
            HintEn: "Join `Submissions` with `Students` and filter `WHERE s.IsPassed = 1 AND s.ExecutionMs < 50`.",
            StarterQuery: "SELECT st.FullName, s.ChallengeTitle, s.ExecutionMs\nFROM Submissions s\nINNER JOIN Students st ON st.Id = s.StudentId\nWHERE s.IsPassed = 1 AND s.ExecutionMs < 50\nORDER BY s.ExecutionMs ASC;",
            ExpectedQuery: "SELECT st.FullName, s.ChallengeTitle, s.ExecutionMs FROM Submissions s INNER JOIN Students st ON st.Id = s.StudentId WHERE s.IsPassed = 1 AND s.ExecutionMs < 50 ORDER BY s.ExecutionMs ASC"),
        new(
            Id: 4,
            Slug: "active-contributors-having",
            TitleVi: "Học viên nộp từ 2 bài đạt trở lên",
            TitleEn: "Learners with 2+ Passed Submissions",
            Difficulty: "Intermediate",
            PromptVi: "Liệt kê `FullName` và `PassedCount` của những học viên có ít nhất 2 bài nộp `IsPassed = 1`.",
            PromptEn: "List `FullName` and `PassedCount` for students with at least 2 passing submissions (`IsPassed = 1`).",
            HintVi: "Dùng `GROUP BY st.Id, st.FullName HAVING COUNT(s.Id) >= 2 ORDER BY PassedCount DESC, st.FullName ASC`.",
            HintEn: "Use `GROUP BY st.Id, st.FullName HAVING COUNT(s.Id) >= 2 ORDER BY PassedCount DESC, st.FullName ASC`.",
            StarterQuery: "SELECT st.FullName, COUNT(s.Id) AS PassedCount\nFROM Students st\nINNER JOIN Submissions s ON s.StudentId = st.Id\nWHERE s.IsPassed = 1\nGROUP BY st.Id, st.FullName\nHAVING COUNT(s.Id) >= 2\nORDER BY PassedCount DESC, st.FullName ASC;",
            ExpectedQuery: "SELECT st.FullName, COUNT(s.Id) AS PassedCount FROM Students st INNER JOIN Submissions s ON s.StudentId = st.Id WHERE s.IsPassed = 1 GROUP BY st.Id, st.FullName HAVING COUNT(s.Id) >= 2 ORDER BY PassedCount DESC, st.FullName ASC"),
        new(
            Id: 5,
            Slug: "cte-streak-leaders",
            TitleVi: "CTE: So sánh XP với mức trung bình toàn hệ thống",
            TitleEn: "CTE: Students Above Average Academy XP",
            Difficulty: "Advanced",
            PromptVi: "Dùng `WITH` (CTE) để tính `AvgXp`, sau đó liệt kê `FullName`, `XpPoints`, `StreakDays` của học viên có `XpPoints > AvgXp`.",
            PromptEn: "Use a `WITH` CTE to compute `AvgXp`, then list `FullName`, `XpPoints`, and `StreakDays` for students above average XP.",
            HintVi: "Khai báo `WITH AcademyStats AS (SELECT AVG(XpPoints) AS AvgXp FROM Students)` rồi join/so sánh.",
            HintEn: "Define `WITH AcademyStats AS (SELECT AVG(XpPoints) AS AvgXp FROM Students)` and filter `XpPoints > AvgXp`.",
            StarterQuery: "WITH AcademyStats AS (\n    SELECT AVG(XpPoints) AS AvgXp FROM Students\n)\nSELECT s.FullName, s.XpPoints, s.StreakDays\nFROM Students s, AcademyStats a\nWHERE s.XpPoints > a.AvgXp\nORDER BY s.XpPoints DESC;",
            ExpectedQuery: "WITH AcademyStats AS (SELECT AVG(XpPoints) AS AvgXp FROM Students) SELECT s.FullName, s.XpPoints, s.StreakDays FROM Students s, AcademyStats a WHERE s.XpPoints > a.AvgXp ORDER BY s.XpPoints DESC")
    ];

    public IReadOnlyList<SqlTableSchemaDto> GetSchemaOverview() => SchemaTables;

    public IReadOnlyList<SqlChallengeDto> GetChallenges() => Challenges;

    public async Task<SqlExecutionResultDto> ExecuteQueryAsync(
        string query,
        int? challengeId = null,
        bool isEnglish = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SqlExecutionResultDto
            {
                IsSuccess = false,
                Error = isEnglish ? "SQL query cannot be empty." : "Câu truy vấn SQL không được để trống."
            };
        }

        if (!TryValidateReadOnlySql(query, isEnglish, out var normalizedSql, out var validationError))
        {
            return new SqlExecutionResultDto
            {
                IsSuccess = false,
                Error = validationError
            };
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(cancellationToken);
            await SeedEphemeralDatabaseAsync(connection, cancellationToken);

            var (columns, rows, truncated) = await RunReadQueryAsync(connection, normalizedSql, cancellationToken);
            sw.Stop();

            var result = new SqlExecutionResultDto
            {
                IsSuccess = true,
                Columns = columns,
                Rows = rows,
                RowCount = rows.Count,
                Truncated = truncated,
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };

            if (challengeId.HasValue)
            {
                var challenge = Challenges.FirstOrDefault(c => c.Id == challengeId.Value);
                if (challenge is not null)
                {
                    var expected = await RunReadQueryAsync(connection, challenge.ExpectedQuery, cancellationToken);
                    bool passed = MatricesMatch(rows, expected.Rows) && columns.Count == expected.Columns.Count;
                    result.ChallengePassed = passed;
                    result.ChallengeMessage = passed
                        ? (isEnglish
                            ? $"Challenge #{challenge.Id} passed! Result set matches all {expected.Rows.Count} expected rows."
                            : $"Xuất sắc! Bạn đã vượt qua Thử thách #{challenge.Id} ({expected.Rows.Count} dòng khớp chính xác).")
                        : (isEnglish
                            ? $"Query ran cleanly, but result set ({rows.Count} rows, {columns.Count} cols) differs from expected ({expected.Rows.Count} rows, {expected.Columns.Count} cols)."
                            : $"Truy vấn chạy thành công, nhưng kết quả ({rows.Count} dòng, {columns.Count} cột) chưa khớp đáp án ({expected.Rows.Count} dòng, {expected.Columns.Count} cột).");
                }
            }

            return result;
        }
        catch (SqliteException ex)
        {
            sw.Stop();
            return new SqlExecutionResultDto
            {
                IsSuccess = false,
                Error = isEnglish ? $"SQL Syntax / Execution Error: {ex.Message}" : $"Lỗi cú pháp SQL: {ex.Message}",
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            return new SqlExecutionResultDto
            {
                IsSuccess = false,
                Error = isEnglish ? $"Sandbox Error: {ex.Message}" : $"Lỗi sandbox thực thi: {ex.Message}",
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };
        }
    }

    public static bool TryValidateReadOnlySql(
        string query,
        bool isEnglish,
        out string normalizedSql,
        out string errorMessage)
    {
        normalizedSql = string.Empty;
        errorMessage = string.Empty;

        if (query.Length > MaxQueryLength)
        {
            errorMessage = isEnglish
                ? $"Query exceeds the {MaxQueryLength}-character sandbox limit."
                : $"Câu truy vấn vượt quá giới hạn {MaxQueryLength} ký tự cho phép.";
            return false;
        }

        // Strip SQL comments and normalize trailing semicolons cleanly before validation
        string stripped = BlockCommentRegex().Replace(query, " ");
        stripped = LineCommentRegex().Replace(stripped, " ");
        stripped = stripped.Trim().TrimEnd(';').Trim();

        if (string.IsNullOrWhiteSpace(stripped))
        {
            errorMessage = isEnglish ? "SQL query contains no executable statement." : "Câu truy vấn SQL không chứa lệnh hợp lệ.";
            return false;
        }

        if (stripped.Contains(';'))
        {
            errorMessage = isEnglish
                ? "Multi-statement SQL queries are blocked in the sandbox. Run one SELECT or WITH query at a time."
                : "Sandbox không cho phép chạy nhiều câu lệnh SQL phân tách bởi dấu chấm phẩy (;).";
            return false;
        }

        if (!StartsWithSelectOrWithRegex().IsMatch(stripped))
        {
            errorMessage = isEnglish
                ? "Read-only sandbox: queries must start with SELECT or WITH."
                : "Sandbox chỉ đọc: câu truy vấn phải bắt đầu bằng SELECT hoặc WITH.";
            return false;
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            if (Regex.IsMatch(stripped, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                errorMessage = isEnglish
                    ? $"Keyword '{keyword}' is blocked in the read-only SQL sandbox."
                    : $"Từ khóa '{keyword}' bị chặn trong môi trường SQL sandbox chỉ đọc.";
                return false;
            }
        }

        normalizedSql = stripped;
        return true;
    }

    private static async Task<(List<string> Columns, List<List<string>> Rows, bool Truncated)> RunReadQueryAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 3;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(i => reader.GetName(i))
            .ToList();

        var rows = new List<List<string>>();
        bool truncated = false;

        while (await reader.ReadAsync(cancellationToken))
        {
            if (rows.Count >= MaxResultRows)
            {
                truncated = true;
                break;
            }

            var row = new List<string>(reader.FieldCount);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row.Add(reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
            }
            rows.Add(row);
        }

        return (columns, rows, truncated);
    }

    private static bool MatricesMatch(List<List<string>> actual, List<List<string>> expected)
    {
        if (actual.Count != expected.Count)
        {
            return false;
        }

        for (int r = 0; r < actual.Count; r++)
        {
            if (actual[r].Count != expected[r].Count)
            {
                return false;
            }

            for (int c = 0; c < actual[r].Count; c++)
            {
                if (!string.Equals(actual[r][c].Trim(), expected[r][c].Trim(), StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static async Task SeedEphemeralDatabaseAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string seedSql = """
            CREATE TABLE Students (
                Id INTEGER PRIMARY KEY,
                FullName TEXT NOT NULL,
                Country TEXT NOT NULL,
                XpPoints INTEGER NOT NULL,
                StreakDays INTEGER NOT NULL,
                JoinedAt TEXT NOT NULL
            );

            CREATE TABLE Tracks (
                Id INTEGER PRIMARY KEY,
                Slug TEXT NOT NULL,
                Title TEXT NOT NULL,
                Difficulty TEXT NOT NULL
            );

            CREATE TABLE Enrollments (
                Id INTEGER PRIMARY KEY,
                StudentId INTEGER NOT NULL,
                TrackId INTEGER NOT NULL,
                CompletionPercent INTEGER NOT NULL,
                EnrolledAt TEXT NOT NULL
            );

            CREATE TABLE Submissions (
                Id INTEGER PRIMARY KEY,
                StudentId INTEGER NOT NULL,
                ChallengeTitle TEXT NOT NULL,
                ExecutionMs INTEGER NOT NULL,
                IsPassed INTEGER NOT NULL,
                SubmittedAt TEXT NOT NULL
            );

            INSERT INTO Students VALUES
                (1, 'Nguyen Van An', 'VN', 2950, 32, '2026-01-10'),
                (2, 'Tran Thi Binh', 'VN', 2410, 21, '2026-02-04'),
                (3, 'Alex Rivera', 'US', 2680, 18, '2026-02-15'),
                (4, 'Sora Takahashi', 'JP', 1840, 14, '2026-03-01'),
                (5, 'Le Hoang Nam', 'VN', 1120, 7, '2026-03-19'),
                (6, 'Maya Lin', 'SG', 640, 3, '2026-04-02');

            INSERT INTO Tracks VALUES
                (1, 'csharp-fundamentals', 'C# 14 Fundamentals', 'Beginner'),
                (2, 'aspnet-core-api', 'ASP.NET Core 10 Web API', 'Intermediate'),
                (3, 'ef-core-postgresql', 'EF Core 10 & PostgreSQL', 'Intermediate'),
                (4, 'cloud-native-microservices', '.NET Aspire & Kubernetes', 'Advanced');

            INSERT INTO Enrollments VALUES
                (1, 1, 1, 100, '2026-01-11'),
                (2, 1, 4, 92, '2026-02-01'),
                (3, 2, 2, 85, '2026-02-06'),
                (4, 3, 2, 95, '2026-02-16'),
                (5, 3, 3, 78, '2026-02-25'),
                (6, 4, 3, 64, '2026-03-03'),
                (7, 5, 1, 70, '2026-03-20'),
                (8, 6, 4, 40, '2026-04-05');

            INSERT INTO Submissions VALUES
                (1, 1, 'Two Sum LINQ', 28, 1, '2026-04-10'),
                (2, 1, 'Async Parallel Fetch', 42, 1, '2026-04-11'),
                (3, 2, 'Binary Search', 35, 1, '2026-04-12'),
                (4, 2, 'Rate Limiter Window', 61, 1, '2026-04-13'),
                (5, 3, 'LRU Cache Generic', 31, 1, '2026-04-14'),
                (6, 3, 'Merge Intervals', 47, 1, '2026-04-15'),
                (7, 4, 'SQL CTE Rank', 88, 0, '2026-04-16'),
                (8, 5, 'FizzBuzz Pattern', 19, 1, '2026-04-17');
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = seedSql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    [GeneratedRegex(@"/\*[\s\S]*?\*/")]
    private static partial Regex BlockCommentRegex();

    [GeneratedRegex(@"--[^\r\n]*")]
    private static partial Regex LineCommentRegex();

    [GeneratedRegex(@"^(SELECT|WITH)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StartsWithSelectOrWithRegex();
}
