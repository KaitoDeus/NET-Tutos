using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly AppDbContext _context;

    public LeaderboardService(AppDbContext context)
    {
        _context = context;
    }

    public List<BadgeCatalogItem> GetAllAvailableBadges()
    {
        return new List<BadgeCatalogItem>
        {
            new()
            {
                BadgeCode = "NEWBIE",
                Title = "Tân Binh .NET",
                Description = "Bắt đầu hành trình và hoàn thành bài học lý thuyết đầu tiên",
                IconClass = "bi-rocket-takeoff-fill",
                ColorClass = "primary",
                ConditionDescription = "Hoàn thành 1 bài học bất kỳ"
            },
            new()
            {
                BadgeCode = "SCHOLAR",
                Title = "Học Giả Chăm Chỉ",
                Description = "Kiên trì tích lũy kiến thức và hoàn thành từ 5 bài học",
                IconClass = "bi-book-half",
                ColorClass = "info",
                ConditionDescription = "Hoàn thành 5 bài học"
            },
            new()
            {
                BadgeCode = "CODER",
                Title = "Thợ Săn Thuật Toán",
                Description = "Thực thi và vượt qua thử thách lập trình C# đầu tiên",
                IconClass = "bi-code-slash",
                ColorClass = "warning",
                ConditionDescription = "Vượt qua 1 thử thách thuật toán"
            },
            new()
            {
                BadgeCode = "ALGO_MASTER",
                Title = "Cao Thủ Thuật Toán",
                Description = "Chinh phục từ 3 thử thách thuật toán trong C# Sandbox",
                IconClass = "bi-lightning-charge-fill",
                ColorClass = "danger",
                ConditionDescription = "Vượt qua 3 thử thách thuật toán"
            },
            new()
            {
                BadgeCode = "CERTIFIED",
                Title = "Bậc Thầy Chứng Chỉ",
                Description = "Vượt qua kỳ thi tốt nghiệp và nhận chứng chỉ số chính thức",
                IconClass = "bi-award-fill",
                ColorClass = "success",
                ConditionDescription = "Đạt chứng chỉ hoàn thành khóa học"
            },
            new()
            {
                BadgeCode = "COMMUNITY_HERO",
                Title = "Người Truyền Lửa",
                Description = "Tích cực đóng góp lời giải và hỗ trợ cộng đồng học viên",
                IconClass = "bi-chat-heart-fill",
                ColorClass = "purple",
                ConditionDescription = "Có bình luận được chọn làm Giải Pháp Đúng hoặc gửi 3 bình luận"
            },
            new()
            {
                BadgeCode = "STREAK_3",
                Title = "Ngọn Lửa Bền Bỉ",
                Description = "Duy trì chuỗi học tập 3 ngày liên tục",
                IconClass = "bi-fire",
                ColorClass = "danger",
                ConditionDescription = "Đạt chuỗi học tập từ 3 ngày liên tục"
            },
            new()
            {
                BadgeCode = "STREAK_7",
                Title = "Chiến Binh Kỷ Luật",
                Description = "Duy trì chuỗi học tập 7 ngày liên tiếp không nghỉ",
                IconClass = "bi-shield-check",
                ColorClass = "warning",
                ConditionDescription = "Đạt chuỗi học tập từ 7 ngày liên tục"
            },
            new()
            {
                BadgeCode = "STREAK_30",
                Title = "Huyền Thoại Bất Bại",
                Description = "Kỷ lục 30 ngày kiên trì học tập liên tục cùng .NET",
                IconClass = "bi-trophy-fill",
                ColorClass = "primary",
                ConditionDescription = "Đạt chuỗi học tập từ 30 ngày liên tục"
            }
        };
    }

    public async Task<LeaderboardViewModel> GetLeaderboardAsync(string? currentUserId, int count = 25)
    {
        var users = await _context.Users
            .Include(u => u.LessonProgresses)
            .Include(u => u.Submissions)
            .Include(u => u.Certificates)
            .Include(u => u.Comments)
            .Include(u => u.Badges)
            .OrderByDescending(u => u.ExperiencePoints)
            .ThenByDescending(u => u.LessonProgresses.Count(p => p.IsCompleted))
            .Take(count)
            .ToListAsync();

        var topUserItems = new List<LeaderboardUserItem>();
        int rank = 1;

        foreach (var u in users)
        {
            var item = new LeaderboardUserItem
            {
                Rank = rank++,
                UserId = u.Id,
                FullName = string.IsNullOrWhiteSpace(u.FullName) ? (u.UserName ?? "Học viên ẩn danh") : u.FullName,
                Email = u.Email ?? "",
                AvatarUrl = u.AvatarUrl,
                ExperiencePoints = u.ExperiencePoints,
                CompletedLessonsCount = u.LessonProgresses.Count(p => p.IsCompleted),
                CompletedChallengesCount = u.Submissions.Where(s => s.IsPassed).Select(s => s.CodingChallengeId).Distinct().Count(),
                CertificatesCount = u.Certificates.Count,
                HelpfulCommentsCount = u.Comments.Count(c => c.IsBestAnswer),
                CurrentStreak = u.CurrentStreak,
                LongestStreak = u.LongestStreak,
                Badges = u.Badges.Select(b => new UserBadgeDto
                {
                    BadgeCode = b.BadgeCode,
                    Title = b.Title,
                    Description = b.Description,
                    IconClass = b.IconClass,
                    ColorClass = b.ColorClass,
                    EarnedAt = b.EarnedAt
                }).ToList()
            };
            topUserItems.Add(item);
        }

        LeaderboardUserItem? currentUserItem = null;
        int? currentUserRank = null;

        if (!string.IsNullOrEmpty(currentUserId))
        {
            var foundInTop = topUserItems.FirstOrDefault(i => i.UserId == currentUserId);
            if (foundInTop != null)
            {
                currentUserRank = foundInTop.Rank;
                currentUserItem = foundInTop;
            }
            else
            {
                var currentUser = await _context.Users
                    .Include(u => u.LessonProgresses)
                    .Include(u => u.Submissions)
                    .Include(u => u.Certificates)
                    .Include(u => u.Comments)
                    .Include(u => u.Badges)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);

                if (currentUser != null)
                {
                    int totalAbove = await _context.Users
                        .CountAsync(u => u.ExperiencePoints > currentUser.ExperiencePoints);
                    currentUserRank = totalAbove + 1;

                    currentUserItem = new LeaderboardUserItem
                    {
                        Rank = currentUserRank.Value,
                        UserId = currentUser.Id,
                        FullName = string.IsNullOrWhiteSpace(currentUser.FullName) ? (currentUser.UserName ?? "Học viên") : currentUser.FullName,
                        Email = currentUser.Email ?? "",
                        AvatarUrl = currentUser.AvatarUrl,
                        ExperiencePoints = currentUser.ExperiencePoints,
                        CompletedLessonsCount = currentUser.LessonProgresses.Count(p => p.IsCompleted),
                        CompletedChallengesCount = currentUser.Submissions.Where(s => s.IsPassed).Select(s => s.CodingChallengeId).Distinct().Count(),
                        CertificatesCount = currentUser.Certificates.Count,
                        HelpfulCommentsCount = currentUser.Comments.Count(c => c.IsBestAnswer),
                        CurrentStreak = currentUser.CurrentStreak,
                        LongestStreak = currentUser.LongestStreak,
                        Badges = currentUser.Badges.Select(b => new UserBadgeDto
                        {
                            BadgeCode = b.BadgeCode,
                            Title = b.Title,
                            Description = b.Description,
                            IconClass = b.IconClass,
                            ColorClass = b.ColorClass,
                            EarnedAt = b.EarnedAt
                        }).ToList()
                    };
                }
            }
        }

        return new LeaderboardViewModel
        {
            TopUsers = topUserItems,
            AllBadges = GetAllAvailableBadges(),
            CurrentUserRank = currentUserRank,
            CurrentUserItem = currentUserItem
        };
    }

    public async Task<List<UserBadgeDto>> GetUserBadgesAsync(string userId)
    {
        var badges = await _context.UserBadges
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();

        return badges.Select(b => new UserBadgeDto
        {
            BadgeCode = b.BadgeCode,
            Title = b.Title,
            Description = b.Description,
            IconClass = b.IconClass,
            ColorClass = b.ColorClass,
            EarnedAt = b.EarnedAt
        }).ToList();
    }

    public async Task CheckAndAwardBadgesAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.LessonProgresses)
            .Include(u => u.Submissions)
            .Include(u => u.Certificates)
            .Include(u => u.Comments)
            .Include(u => u.Badges)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        var existingCodes = user.Badges.Select(b => b.BadgeCode).ToHashSet();
        var catalog = GetAllAvailableBadges().ToDictionary(b => b.BadgeCode);

        void TryAward(string code, bool conditionMet)
        {
            if (conditionMet && !existingCodes.Contains(code) && catalog.TryGetValue(code, out var badgeDef))
            {
                _context.UserBadges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeCode = badgeDef.BadgeCode,
                    Title = badgeDef.Title,
                    Description = badgeDef.Description,
                    IconClass = badgeDef.IconClass,
                    ColorClass = badgeDef.ColorClass,
                    EarnedAt = DateTime.UtcNow
                });
                existingCodes.Add(code);
            }
        }

        // 1. Tân Binh .NET: Completed at least 1 lesson
        int completedLessons = user.LessonProgresses.Count(p => p.IsCompleted);
        TryAward("NEWBIE", completedLessons >= 1);

        // 2. Học Giả Chăm Chỉ: Completed at least 5 lessons
        TryAward("SCHOLAR", completedLessons >= 5);

        // 3. Thợ Săn Thuật Toán: Passed at least 1 coding challenge
        int passedChallenges = user.Submissions.Where(s => s.IsPassed).Select(s => s.CodingChallengeId).Distinct().Count();
        TryAward("CODER", passedChallenges >= 1);

        // 4. Cao Thủ Thuật Toán: Passed at least 3 coding challenges
        TryAward("ALGO_MASTER", passedChallenges >= 3);

        // 5. Bậc Thầy Chứng Chỉ: At least 1 certificate
        TryAward("CERTIFIED", user.Certificates.Any());

        // 6. Người Truyền Lửa: Has best answer or 3+ comments
        bool isHero = user.Comments.Any(c => c.IsBestAnswer) || user.Comments.Count >= 3;
        TryAward("COMMUNITY_HERO", isHero);

        await _context.SaveChangesAsync();
    }
}
