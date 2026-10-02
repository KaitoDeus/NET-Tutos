using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class StreakService : IStreakService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly IActivityFeedService _activityFeedService;
    private readonly ILogger<StreakService> _logger;

    public StreakService(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService,
        IActivityFeedService activityFeedService,
        ILogger<StreakService> logger)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
        _activityFeedService = activityFeedService;
        _logger = logger;
    }

    public async Task<StreakStatusViewModel> GetUserStreakStatusAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new StreakStatusViewModel
            {
                IsAuthenticated = false,
                CurrentStreak = 0,
                LongestStreak = 0,
                HasCheckedInToday = false,
                NextRewardXp = 10,
                Past7Days = BuildEmptyPast7Days(DateTime.UtcNow.Date),
                Milestones = BuildMilestones(0)
            };
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new StreakStatusViewModel
            {
                IsAuthenticated = false,
                CurrentStreak = 0,
                LongestStreak = 0,
                HasCheckedInToday = false,
                NextRewardXp = 10,
                Past7Days = BuildEmptyPast7Days(DateTime.UtcNow.Date),
                Milestones = BuildMilestones(0)
            };
        }

        var today = DateTime.UtcNow.Date;
        var hasCheckedInToday = user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date == today;

        // Determine effective display streak
        int effectiveStreak = user.CurrentStreak;
        if (!hasCheckedInToday && user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date < today.AddDays(-1))
        {
            // Streak lapsed because user missed yesterday
            effectiveStreak = 0;
        }

        // Get past 7 days check-in records
        var startDate = today.AddDays(-6);
        var checkIns = await _context.DailyCheckIns
            .Where(d => d.UserId == userId && d.CheckInDate >= startDate && d.CheckInDate <= today)
            .ToListAsync();

        var past7Days = new List<StreakDayItem>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var record = checkIns.FirstOrDefault(c => c.CheckInDate.Date == date);
            past7Days.Add(new StreakDayItem
            {
                Date = date,
                DayName = GetVietnameseDayName(date.DayOfWeek),
                IsCheckedIn = record != null,
                IsToday = date == today,
                XpEarned = record?.XpEarned ?? 0
            });
        }

        int totalXpFromStreaks = await _context.DailyCheckIns
            .Where(d => d.UserId == userId)
            .SumAsync(d => d.XpEarned);

        int nextStreakDay = hasCheckedInToday ? effectiveStreak + 1 : (effectiveStreak == 0 ? 1 : effectiveStreak + 1);
        int nextRewardXp = CalculateXpReward(nextStreakDay);

        return new StreakStatusViewModel
        {
            IsAuthenticated = true,
            CurrentStreak = effectiveStreak,
            LongestStreak = user.LongestStreak,
            HasCheckedInToday = hasCheckedInToday,
            LastCheckInDate = user.LastCheckInDate,
            NextRewardXp = nextRewardXp,
            TotalXpEarnedFromStreaks = totalXpFromStreaks,
            Past7Days = past7Days,
            Milestones = BuildMilestones(user.LongestStreak)
        };
    }

    public async Task<StreakCheckInResult> CheckInTodayAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new StreakCheckInResult
            {
                Success = false,
                Message = "Không tìm thấy thông tin tài khoản người dùng."
            };
        }

        var today = DateTime.UtcNow.Date;

        // Check if already checked in today
        if (user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date == today)
        {
            var todayRecord = await _context.DailyCheckIns
                .FirstOrDefaultAsync(d => d.UserId == userId && d.CheckInDate.Date == today);

            var past7 = await GetPast7DaysListAsync(userId, today);

            return new StreakCheckInResult
            {
                Success = true,
                IsAlreadyCheckedInToday = true,
                CurrentStreak = user.CurrentStreak,
                LongestStreak = user.LongestStreak,
                XpEarned = todayRecord?.XpEarned ?? 0,
                Message = $"Hôm nay bạn đã điểm danh rồi! Chuỗi học tập hiện tại: {user.CurrentStreak} ngày liên tiếp.",
                Past7Days = past7
            };
        }

        // Calculate new streak count
        int newStreak;
        if (user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date == today.AddDays(-1))
        {
            newStreak = user.CurrentStreak + 1;
        }
        else
        {
            newStreak = 1;
        }

        int xpReward = CalculateXpReward(newStreak);

        user.CurrentStreak = newStreak;
        user.LongestStreak = Math.Max(user.LongestStreak, newStreak);
        user.LastCheckInDate = today;
        user.ExperiencePoints += xpReward;

        var checkInRecord = new DailyCheckIn
        {
            UserId = userId,
            CheckInDate = today,
            StreakDay = newStreak,
            XpEarned = xpReward,
            CreatedAt = DateTime.UtcNow
        };
        _context.DailyCheckIns.Add(checkInRecord);

        // Check and award badges
        var newlyUnlocked = new List<string>();

        if (newStreak >= 3 && !await _context.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeCode == "STREAK_3"))
        {
            _context.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeCode = "STREAK_3",
                Title = "Ngọn Lửa Bền Bỉ",
                Description = "Duy trì chuỗi học tập 3 ngày liên tục",
                IconClass = "bi-fire",
                ColorClass = "danger",
                EarnedAt = DateTime.UtcNow
            });
            user.ExperiencePoints += 30;
            newlyUnlocked.Add("Mở khóa huy hiệu: Ngọn Lửa Bền Bỉ (+30 XP thưởng)");
        }

        if (newStreak >= 7 && !await _context.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeCode == "STREAK_7"))
        {
            _context.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeCode = "STREAK_7",
                Title = "Chiến Binh Kỷ Luật",
                Description = "Duy trì chuỗi học tập 7 ngày liên tiếp không nghỉ",
                IconClass = "bi-shield-check",
                ColorClass = "warning",
                EarnedAt = DateTime.UtcNow
            });
            user.ExperiencePoints += 70;
            newlyUnlocked.Add("Mở khóa huy hiệu: Chiến Binh Kỷ Luật (+70 XP thưởng)");
        }

        if (newStreak >= 30 && !await _context.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeCode == "STREAK_30"))
        {
            _context.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeCode = "STREAK_30",
                Title = "Huyền Thoại Bất Bại",
                Description = "Kỷ lục 30 ngày kiên trì học tập liên tục cùng .NET",
                IconClass = "bi-trophy-fill",
                ColorClass = "primary",
                EarnedAt = DateTime.UtcNow
            });
            user.ExperiencePoints += 200;
            newlyUnlocked.Add("Mở khóa huy hiệu: Huyền Thoại Bất Bại (+200 XP thưởng)");
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} checked in successfully. Streak: {Streak}, XP: +{Xp}", userId, newStreak, xpReward);

        try
        {
            await _notificationService.CreateNotificationAsync(
                userId,
                "Điểm danh nhận thưởng thành công! 🔥",
                $"Bạn đã duy trì chuỗi {newStreak} ngày liên tiếp (+{xpReward} XP)!",
                NotificationType.StreakReminder,
                "/Streak");

            if (newStreak >= 3)
            {
                await _activityFeedService.RecordActivityAsync(
                    userId,
                    ActivityType.StreakAchieved,
                    "đã duy trì chuỗi học tập",
                    $"{newStreak} ngày liên tiếp kiên trì không nghỉ 🔥",
                    "/Streak",
                    xpEarned: xpReward);
            }

            foreach (var badgeMsg in newlyUnlocked)
            {
                await _notificationService.CreateNotificationAsync(
                    userId,
                    "Huy hiệu mới đã mở khóa! 🏆",
                    badgeMsg,
                    NotificationType.BadgeEarned,
                    "/Leaderboard");

                await _activityFeedService.RecordActivityAsync(
                    userId,
                    ActivityType.BadgeEarned,
                    "đã mở khóa thành tích mới",
                    badgeMsg,
                    "/Leaderboard");
            }
        }
        catch { }

        var past7Days = await GetPast7DaysListAsync(userId, today);

        string message = newStreak > 1
            ? $"Tuyệt vời! Bạn đã duy trì chuỗi {newStreak} ngày liên tiếp và nhận được +{xpReward} XP!"
            : $"Chào mừng trở lại! Bắt đầu chuỗi học tập mới với +{xpReward} XP!";

        return new StreakCheckInResult
        {
            Success = true,
            IsAlreadyCheckedInToday = false,
            CurrentStreak = newStreak,
            LongestStreak = user.LongestStreak,
            XpEarned = xpReward,
            Message = message,
            NewlyUnlockedBadges = newlyUnlocked,
            Past7Days = past7Days
        };
    }

    public async Task<StreakCheckInResult?> RecordLearningActivityStreakAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var today = DateTime.UtcNow.Date;
        if (user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date == today)
        {
            // Already checked in today, no change
            return null;
        }

        // Auto check-in when doing a learning activity
        return await CheckInTodayAsync(userId);
    }

    private async Task<List<StreakDayItem>> GetPast7DaysListAsync(string userId, DateTime today)
    {
        var startDate = today.AddDays(-6);
        var checkIns = await _context.DailyCheckIns
            .Where(d => d.UserId == userId && d.CheckInDate >= startDate && d.CheckInDate <= today)
            .ToListAsync();

        var past7Days = new List<StreakDayItem>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var record = checkIns.FirstOrDefault(c => c.CheckInDate.Date == date);
            past7Days.Add(new StreakDayItem
            {
                Date = date,
                DayName = GetVietnameseDayName(date.DayOfWeek),
                IsCheckedIn = record != null,
                IsToday = date == today,
                XpEarned = record?.XpEarned ?? 0
            });
        }
        return past7Days;
    }

    private static List<StreakDayItem> BuildEmptyPast7Days(DateTime today)
    {
        var list = new List<StreakDayItem>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            list.Add(new StreakDayItem
            {
                Date = date,
                DayName = GetVietnameseDayName(date.DayOfWeek),
                IsCheckedIn = false,
                IsToday = date == today,
                XpEarned = 0
            });
        }
        return list;
    }

    private static List<StreakMilestoneItem> BuildMilestones(int longestStreak)
    {
        return new List<StreakMilestoneItem>
        {
            new() { Days = 3, Title = "Ngọn Lửa Bền Bỉ", BonusXp = 30, BadgeCode = "STREAK_3", IsReached = longestStreak >= 3 },
            new() { Days = 7, Title = "Chiến Binh Kỷ Luật", BonusXp = 70, BadgeCode = "STREAK_7", IsReached = longestStreak >= 7 },
            new() { Days = 30, Title = "Huyền Thoại Bất Bại", BonusXp = 200, BadgeCode = "STREAK_30", IsReached = longestStreak >= 30 }
        };
    }

    private static int CalculateXpReward(int streakDay)
    {
        return streakDay switch
        {
            1 => 10,
            2 => 15,
            3 => 20,
            4 => 20,
            5 => 25,
            6 => 25,
            7 => 50, // 1 tuần liên tục
            14 => 100, // 2 tuần
            30 => 250, // 1 tháng
            _ => streakDay % 7 == 0 ? 50 : 20
        };
    }

    private static string GetVietnameseDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "T2",
        DayOfWeek.Tuesday => "T3",
        DayOfWeek.Wednesday => "T4",
        DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6",
        DayOfWeek.Saturday => "T7",
        DayOfWeek.Sunday => "CN",
        _ => ""
    };
}
