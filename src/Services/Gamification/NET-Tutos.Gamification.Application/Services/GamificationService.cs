using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NET_Tutos.Gamification.Application.Common.Interfaces;
using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Domain.Entities;

namespace NET_Tutos.Gamification.Application.Services;

public class GamificationService : IGamificationService
{
    private readonly IGamificationDbContext _context;
    private readonly ILogger<GamificationService> _logger;

    public GamificationService(IGamificationDbContext context, ILogger<GamificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StreakDto> GetUserStreakAsync(string userId)
    {
        var streak = await _context.Streaks.FirstOrDefaultAsync(s => s.UserId == userId);
        if (streak == null)
        {
            return new StreakDto
            {
                UserId = userId,
                CurrentStreak = 0,
                LongestStreak = 0,
                DaysToNextMilestone = 3,
                IsCheckedInToday = false
            };
        }

        var today = DateTime.UtcNow.Date;
        bool isCheckedInToday = streak.LastCheckInDate.HasValue && streak.LastCheckInDate.Value.Date == today;

        int daysToNext = CalculateDaysToNextMilestone(streak.CurrentStreak);

        return new StreakDto
        {
            UserId = userId,
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            DaysToNextMilestone = daysToNext,
            IsCheckedInToday = isCheckedInToday,
            LastCheckInDate = streak.LastCheckInDate
        };
    }

    public async Task<CheckInResultDto> CheckInTodayAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        var streak = await _context.Streaks.FirstOrDefaultAsync(s => s.UserId == userId);

        if (streak == null)
        {
            streak = new UserStreak
            {
                UserId = userId,
                CurrentStreak = 1,
                LongestStreak = 1,
                LastCheckInDate = today,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Streaks.Add(streak);
        }
        else
        {
            if (streak.LastCheckInDate.HasValue && streak.LastCheckInDate.Value.Date == today)
            {
                return new CheckInResultDto
                {
                    IsSuccess = false,
                    Message = "Bạn đã điểm danh ngày hôm nay rồi. Hãy quay lại vào ngày mai!",
                    CurrentStreak = streak.CurrentStreak,
                    XpEarned = 0
                };
            }

            var yesterday = today.AddDays(-1);
            if (streak.LastCheckInDate.HasValue && streak.LastCheckInDate.Value.Date == yesterday)
            {
                streak.CurrentStreak += 1;
            }
            else
            {
                streak.CurrentStreak = 1;
            }

            if (streak.CurrentStreak > streak.LongestStreak)
            {
                streak.LongestStreak = streak.CurrentStreak;
            }

            streak.LastCheckInDate = today;
            streak.UpdatedAt = DateTime.UtcNow;
        }

        // Calculate XP earned: 10 base XP + streak bonus (min 40)
        int streakBonus = Math.Min(streak.CurrentStreak * 2, 40);
        int totalXpEarned = 10 + streakBonus;

        _context.CheckIns.Add(new DailyCheckIn
        {
            UserId = userId,
            CheckInDate = DateTime.UtcNow,
            XpEarned = totalXpEarned,
            StreakCountAtTime = streak.CurrentStreak
        });

        await AwardXpInternalAsync(userId, totalXpEarned);

        // Check milestone badges
        var unlockedBadges = await CheckAndAwardMilestoneBadgesAsync(userId, streak.CurrentStreak);

        await _context.SaveChangesAsync();

        return new CheckInResultDto
        {
            IsSuccess = true,
            Message = $"Điểm danh thành công! Bạn nhận được +{totalXpEarned} XP (Chuỗi {streak.CurrentStreak} ngày).",
            CurrentStreak = streak.CurrentStreak,
            XpEarned = totalXpEarned,
            NewlyUnlockedBadges = unlockedBadges
        };
    }

    public async Task<UserProgressDto> AwardXpAsync(string userId, int xpAmount, string reason)
    {
        var progress = await AwardXpInternalAsync(userId, xpAmount);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Awarded {Amount} XP to User {UserId}. Reason: {Reason}", xpAmount, userId, reason);
        return progress;
    }

    public async Task<List<BadgeDto>> GetBadgesAsync(string? userId = null)
    {
        var allBadges = await _context.Badges.AsNoTracking().ToListAsync();
        var userBadgeMap = new Dictionary<int, DateTime>();

        if (!string.IsNullOrEmpty(userId))
        {
            var userBadges = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            userBadgeMap = userBadges.ToDictionary(ub => ub.BadgeId, ub => ub.UnlockedAt);
        }

        return allBadges.Select(b => new BadgeDto
        {
            Id = b.Id,
            Code = b.Code,
            Name = b.Name,
            Description = b.Description,
            IconClass = b.IconClass,
            BadgeColor = b.BadgeColor,
            UnlockedAt = userBadgeMap.TryGetValue(b.Id, out var dt) ? dt : null
        }).ToList();
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int top = 50)
    {
        var records = await _context.XpRecords
            .AsNoTracking()
            .OrderByDescending(r => r.TotalXp)
            .Take(top)
            .ToListAsync();

        var userIds = records.Select(r => r.UserId).ToList();
        var streaks = await _context.Streaks
            .AsNoTracking()
            .Where(s => userIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId, s => s.CurrentStreak);

        int rank = 1;
        return records.Select(r => new LeaderboardEntryDto
        {
            Rank = rank++,
            UserId = r.UserId,
            TotalXp = r.TotalXp,
            CurrentLevel = r.CurrentLevel,
            CurrentStreak = streaks.TryGetValue(r.UserId, out var cur) ? cur : 0
        }).ToList();
    }

    private async Task<UserProgressDto> AwardXpInternalAsync(string userId, int xpAmount)
    {
        var record = await _context.XpRecords.FirstOrDefaultAsync(r => r.UserId == userId);
        if (record == null)
        {
            record = new UserXpRecord
            {
                UserId = userId,
                TotalXp = 0,
                CurrentLevel = 1,
                LastUpdated = DateTime.UtcNow
            };
            _context.XpRecords.Add(record);
        }

        record.TotalXp += xpAmount;
        record.CurrentLevel = (record.TotalXp / 100) + 1;
        record.LastUpdated = DateTime.UtcNow;

        int currentLevelFloorXp = (record.CurrentLevel - 1) * 100;
        int nextLevelXp = record.CurrentLevel * 100;
        int xpInCurrentLevel = record.TotalXp - currentLevelFloorXp;
        double progressPercent = Math.Clamp((double)xpInCurrentLevel / 100 * 100.0, 0, 100);

        return new UserProgressDto
        {
            UserId = userId,
            TotalXp = record.TotalXp,
            CurrentLevel = record.CurrentLevel,
            ProgressToNextLevelPercent = progressPercent,
            XpForNextLevel = nextLevelXp
        };
    }

    private async Task<List<BadgeDto>> CheckAndAwardMilestoneBadgesAsync(string userId, int streakDays)
    {
        var newlyUnlocked = new List<BadgeDto>();
        string? targetBadgeCode = streakDays switch
        {
            >= 30 => "streak-30",
            >= 14 => "streak-14",
            >= 7 => "streak-7",
            >= 3 => "streak-3",
            _ => null
        };

        if (targetBadgeCode == null) return newlyUnlocked;

        var badge = await _context.Badges.FirstOrDefaultAsync(b => b.Code == targetBadgeCode);
        if (badge == null) return newlyUnlocked;

        bool alreadyHas = await _context.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badge.Id);
        if (!alreadyHas)
        {
            _context.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeId = badge.Id,
                UnlockedAt = DateTime.UtcNow
            });

            newlyUnlocked.Add(new BadgeDto
            {
                Id = badge.Id,
                Code = badge.Code,
                Name = badge.Name,
                Description = badge.Description,
                IconClass = badge.IconClass,
                BadgeColor = badge.BadgeColor,
                UnlockedAt = DateTime.UtcNow
            });
        }

        return newlyUnlocked;
    }

    private static int CalculateDaysToNextMilestone(int streak)
    {
        return streak switch
        {
            < 3 => 3 - streak,
            < 7 => 7 - streak,
            < 14 => 14 - streak,
            < 30 => 30 - streak,
            _ => 0
        };
    }
}
