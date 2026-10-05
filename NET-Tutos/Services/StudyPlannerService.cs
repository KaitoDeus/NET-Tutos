using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace NET_Tutos.Services;

public class StudyPlannerService : IStudyPlannerService
{
    private readonly AppDbContext _context;
    private readonly ICurriculumService _curriculumService;
    private readonly IStreakService _streakService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<StudyPlannerService> _logger;

    public StudyPlannerService(
        AppDbContext context,
        ICurriculumService curriculumService,
        IStreakService streakService,
        UserManager<ApplicationUser> userManager,
        ILogger<StudyPlannerService> logger)
    {
        _context = context;
        _curriculumService = curriculumService;
        _streakService = streakService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<StudyPlannerDashboardViewModel> GetDashboardAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;

        var streakStatus = await _streakService.GetUserStreakStatusAsync(userId);

        var activePlan = await _context.StudyPlans
            .Include(sp => sp.PlanItems)
            .Where(sp => sp.UserId == userId && sp.IsActive)
            .OrderByDescending(sp => sp.CreatedAt)
            .FirstOrDefaultAsync();

        if (activePlan == null)
        {
            return new StudyPlannerDashboardViewModel
            {
                HasActivePlan = false,
                CurrentStreak = streakStatus.CurrentStreak,
                LongestStreak = streakStatus.LongestStreak
            };
        }

        var allLessons = await _curriculumService.GetAllLessonsAsync();
        var lessonMap = allLessons.ToDictionary(l => l.LessonNumber);

        var itemsDto = new List<StudyPlanItemDto>();
        foreach (var item in activePlan.PlanItems.OrderBy(pi => pi.ScheduledDate).ThenBy(pi => pi.LessonNumber))
        {
            lessonMap.TryGetValue(item.LessonNumber, out var lesson);

            bool isToday = item.ScheduledDate.Date == today;
            bool isOverdue = !item.IsCompleted && item.ScheduledDate.Date < today;

            itemsDto.Add(new StudyPlanItemDto
            {
                Id = item.Id,
                LessonNumber = item.LessonNumber,
                LessonTitleVi = lesson?.TitleVi ?? $"Bài {item.LessonNumber}",
                LessonTitleEn = lesson?.TitleEn ?? $"Lesson {item.LessonNumber}",
                SectionTitleVi = lesson?.SectionTitleVi ?? "Khóa học",
                SectionTitleEn = lesson?.SectionTitleEn ?? "Curriculum",
                SectionBadgeColor = lesson?.SectionBadgeColor ?? "primary",
                SectionIcon = lesson?.SectionIcon ?? "bi-laptop",
                Difficulty = lesson?.Difficulty ?? DifficultyLevel.Beginner,
                EstimatedMinutes = lesson?.EstimatedMinutes ?? 20,
                ScheduledDate = item.ScheduledDate,
                IsCompleted = item.IsCompleted,
                CompletedAt = item.CompletedAt,
                IsToday = isToday,
                IsOverdue = isOverdue,
                MatchingTutorialSlug = lesson?.MatchingTutorialSlug,
                KeyTags = lesson?.KeyTags ?? new()
            });
        }

        var todayLessons = itemsDto.Where(i => i.IsToday).ToList();
        var overdueLessons = itemsDto.Where(i => i.IsOverdue).ToList();

        // Group into calendar weeks starting from the plan's StartDate
        var weeks = new List<StudyPlanWeekGroup>();
        if (itemsDto.Any())
        {
            var planStart = activePlan.StartDate.Date;
            var planEnd = activePlan.TargetEndDate.Date;
            var minDate = itemsDto.Min(i => i.ScheduledDate.Date);
            var maxDate = itemsDto.Max(i => i.ScheduledDate.Date);

            // Group by 7-day windows
            int weekNumber = 1;
            var currentWeekStart = minDate;
            while (currentWeekStart <= maxDate)
            {
                var currentWeekEnd = currentWeekStart.AddDays(6);
                var weekItems = itemsDto.Where(i => i.ScheduledDate.Date >= currentWeekStart && i.ScheduledDate.Date <= currentWeekEnd).ToList();

                if (weekItems.Any())
                {
                    bool isCurrentWeek = today >= currentWeekStart && today <= currentWeekEnd;
                    weeks.Add(new StudyPlanWeekGroup
                    {
                        WeekNumber = weekNumber,
                        StartDate = currentWeekStart,
                        EndDate = currentWeekEnd,
                        Items = weekItems,
                        IsCurrentWeek = isCurrentWeek
                    });
                }

                currentWeekStart = currentWeekStart.AddDays(7);
                weekNumber++;
            }
        }

        int totalCount = itemsDto.Count;
        int completedCount = itemsDto.Count(i => i.IsCompleted);
        int progress = totalCount > 0 ? (int)Math.Round((double)completedCount / totalCount * 100) : 0;
        int totalDays = Math.Max(1, (int)(activePlan.TargetEndDate.Date - activePlan.StartDate.Date).TotalDays);
        int daysRemaining = Math.Max(0, (int)(activePlan.TargetEndDate.Date - today).TotalDays);

        // Streak danger: if today is a study day with scheduled uncompleted items and user has not learned today
        bool isStreakDanger = todayLessons.Any(l => !l.IsCompleted) && !streakStatus.HasCheckedInToday;

        return new StudyPlannerDashboardViewModel
        {
            HasActivePlan = true,
            PlanId = activePlan.Id,
            Title = activePlan.Title,
            TrackType = activePlan.TrackType,
            FocusType = activePlan.FocusType,
            TargetMinutesPerDay = activePlan.TargetMinutesPerDay,
            StartDate = activePlan.StartDate,
            TargetEndDate = activePlan.TargetEndDate,
            DaysRemaining = daysRemaining,
            TotalDays = totalDays,
            TotalLessons = totalCount,
            CompletedLessons = completedCount,
            ProgressPercentage = progress,
            CurrentStreak = streakStatus.CurrentStreak,
            LongestStreak = streakStatus.LongestStreak,
            IsStreakInDanger = isStreakDanger,
            TodayLessons = todayLessons,
            OverdueLessons = overdueLessons,
            Weeks = weeks
        };
    }

    public async Task<bool> CreatePlanAsync(string userId, CreateStudyPlanRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        // Deactivate previous active plans
        var existingPlans = await _context.StudyPlans
            .Where(sp => sp.UserId == userId && sp.IsActive)
            .ToListAsync();

        foreach (var p in existingPlans)
        {
            p.IsActive = false;
        }

        var today = DateTime.UtcNow.Date;
        int durationDays = request.TrackType switch
        {
            StudyPlanTrack.FastTrack => 30,
            StudyPlanTrack.Standard => 90,
            StudyPlanTrack.SteadyPartTime => 180,
            StudyPlanTrack.Custom => Math.Max(7, request.CustomDurationDays),
            _ => 90
        };

        var targetEnd = today.AddDays(durationDays);

        var daysOfWeekSet = new HashSet<DayOfWeek>();
        if (request.SelectedDays != null && request.SelectedDays.Any())
        {
            foreach (var d in request.SelectedDays)
            {
                // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
                // Request days: 1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat, 7=Sun
                var dow = d == 7 ? DayOfWeek.Sunday : (DayOfWeek)d;
                daysOfWeekSet.Add(dow);
            }
        }
        else
        {
            // Default Monday to Friday
            daysOfWeekSet = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        }

        var newPlan = new StudyPlan
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Kế hoạch C# & .NET 10 Làm chủ Công nghệ" : request.Title.Trim(),
            TrackType = request.TrackType,
            FocusType = request.FocusType,
            TargetMinutesPerDay = request.TargetMinutesPerDay,
            SelectedDaysOfWeek = string.Join(",", request.SelectedDays ?? new() { 1, 2, 3, 4, 5 }),
            StartDate = today,
            TargetEndDate = targetEnd,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Determine list of lesson numbers to study
        var allLessons = await _curriculumService.GetAllLessonsAsync();
        List<int> targetLessons;

        if (request.FocusType == StudyPlanFocus.FundamentalsOnly)
        {
            // Sections 1 and 2 (Lessons 1..43)
            targetLessons = allLessons.Where(l => l.SectionNumber <= 2).Select(l => l.LessonNumber).OrderBy(x => x).ToList();
        }
        else if (request.FocusType == StudyPlanFocus.BackendApiSpecialist)
        {
            // OOP (Section 2), Networking (Section 3), Database (Section 4), ASP.NET Core & MVC (Sections 5 & 7)
            targetLessons = allLessons.Where(l => l.SectionNumber == 2 || l.SectionNumber == 3 || l.SectionNumber == 4 || l.SectionNumber == 5 || l.SectionNumber == 7)
                                      .Select(l => l.LessonNumber).OrderBy(x => x).ToList();
        }
        else
        {
            // All 101 lessons
            targetLessons = allLessons.Select(l => l.LessonNumber).OrderBy(x => x).ToList();
        }

        // Generate valid study dates
        var studyDates = new List<DateTime>();
        var curr = today;
        while (curr <= targetEnd)
        {
            if (daysOfWeekSet.Contains(curr.DayOfWeek))
            {
                studyDates.Add(curr);
            }
            curr = curr.AddDays(1);
        }

        if (!studyDates.Any())
        {
            // Fallback: every day
            curr = today;
            while (curr <= targetEnd)
            {
                studyDates.Add(curr);
                curr = curr.AddDays(1);
            }
        }

        // Distribute lessons onto study dates
        int lessonIndex = 0;
        int totalTargetLessons = targetLessons.Count;
        int totalStudyDays = studyDates.Count;

        for (int dayIdx = 0; dayIdx < totalStudyDays; dayIdx++)
        {
            if (lessonIndex >= totalTargetLessons) break;

            // Calculate how many lessons this day should take
            int remainingLessons = totalTargetLessons - lessonIndex;
            int remainingDays = totalStudyDays - dayIdx;
            int lessonsForThisDay = Math.Max(1, (int)Math.Ceiling((double)remainingLessons / remainingDays));

            for (int k = 0; k < lessonsForThisDay && lessonIndex < totalTargetLessons; k++)
            {
                int lessonNum = targetLessons[lessonIndex++];
                newPlan.PlanItems.Add(new StudyPlanItem
                {
                    LessonNumber = lessonNum,
                    ScheduledDate = studyDates[dayIdx],
                    IsCompleted = false
                });
            }
        }

        // In case there are remaining lessons, put on the last study date
        while (lessonIndex < totalTargetLessons)
        {
            newPlan.PlanItems.Add(new StudyPlanItem
            {
                LessonNumber = targetLessons[lessonIndex++],
                ScheduledDate = studyDates.Last(),
                IsCompleted = false
            });
        }

        _context.StudyPlans.Add(newPlan);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ToggleItemResponse> ToggleItemCompletionAsync(string userId, int itemId)
    {
        var item = await _context.StudyPlanItems
            .Include(pi => pi.StudyPlan)
            .FirstOrDefaultAsync(pi => pi.Id == itemId && pi.StudyPlan != null && pi.StudyPlan.UserId == userId);

        if (item == null)
        {
            return new ToggleItemResponse { Success = false, Message = "Không tìm thấy bài học trong kế hoạch" };
        }

        item.IsCompleted = !item.IsCompleted;
        item.CompletedAt = item.IsCompleted ? DateTime.UtcNow : null;

        int xpAwarded = 0;
        if (item.IsCompleted)
        {
            xpAwarded = 25;
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.ExperiencePoints += xpAwarded;
                await _userManager.UpdateAsync(user);
            }

            try
            {
                await _streakService.RecordLearningActivityStreakAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi duy trì chuỗi học tập khi hoàn thành bài học trong kế hoạch");
            }
        }

        await _context.SaveChangesAsync();

        var planItems = await _context.StudyPlanItems
            .Where(pi => pi.StudyPlanId == item.StudyPlanId)
            .ToListAsync();

        int totalCount = planItems.Count;
        int completedCount = planItems.Count(pi => pi.IsCompleted);
        int percent = totalCount > 0 ? (int)Math.Round((double)completedCount / totalCount * 100) : 0;

        var streakStatus = await _streakService.GetUserStreakStatusAsync(userId);

        return new ToggleItemResponse
        {
            Success = true,
            IsCompleted = item.IsCompleted,
            CompletedCount = completedCount,
            ProgressPercentage = percent,
            CurrentStreak = streakStatus.CurrentStreak,
            XpEarned = xpAwarded,
            Message = item.IsCompleted ? $"+{xpAwarded} XP! Đã hoàn thành bài học và duy trì Streak!" : "Đã hủy đánh dấu hoàn thành"
        };
    }

    public async Task<bool> ReschedulePlanAsync(string userId)
    {
        var activePlan = await _context.StudyPlans
            .Include(sp => sp.PlanItems)
            .Where(sp => sp.UserId == userId && sp.IsActive)
            .FirstOrDefaultAsync();

        if (activePlan == null) return false;

        var today = DateTime.UtcNow.Date;

        // Incomplete items that are scheduled before today
        var overdueItems = activePlan.PlanItems
            .Where(pi => !pi.IsCompleted && pi.ScheduledDate.Date < today)
            .OrderBy(pi => pi.LessonNumber)
            .ToList();

        if (!overdueItems.Any()) return true;

        // Parse selected days of week
        var daysOfWeekSet = new HashSet<DayOfWeek>();
        if (!string.IsNullOrEmpty(activePlan.SelectedDaysOfWeek))
        {
            var parts = activePlan.SelectedDaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (int.TryParse(p, out int d))
                {
                    daysOfWeekSet.Add(d == 7 ? DayOfWeek.Sunday : (DayOfWeek)d);
                }
            }
        }
        if (!daysOfWeekSet.Any())
        {
            daysOfWeekSet = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        }

        // Shift overdue items to future valid days starting from today
        var curr = today;
        int itemIdx = 0;
        while (itemIdx < overdueItems.Count)
        {
            if (daysOfWeekSet.Contains(curr.DayOfWeek))
            {
                overdueItems[itemIdx].ScheduledDate = curr;
                itemIdx++;
            }
            curr = curr.AddDays(1);
        }

        // If target end date was exceeded, extend it
        if (curr > activePlan.TargetEndDate)
        {
            activePlan.TargetEndDate = curr.AddDays(7);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetPlanAsync(string userId)
    {
        var activePlans = await _context.StudyPlans
            .Where(sp => sp.UserId == userId && sp.IsActive)
            .ToListAsync();

        foreach (var p in activePlans)
        {
            p.IsActive = false;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
