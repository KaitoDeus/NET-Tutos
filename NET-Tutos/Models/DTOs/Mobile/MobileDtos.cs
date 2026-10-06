namespace NET_Tutos.Models.DTOs.Mobile;

#region Authentication DTOs
public class MobileLoginRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class MobileRegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class MobileAuthResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public MobileUserDto? User { get; set; }
}

public class MobileUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int TotalXp { get; set; }
    public int Level { get; set; }
    public int StreakDays { get; set; }
    public int CompletedLessonsCount { get; set; }
}
#endregion

#region Tutorials & Categories DTOs
public class MobileCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "primary";
    public string IconClass { get; set; } = "bi-book";
    public int TutorialCount { get; set; }
}

public class MobileTutorialSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int EstimatedReadingMinutes { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int ViewCount { get; set; }
    public bool IsCompleted { get; set; }
}

public class MobileTutorialDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public int EstimatedReadingMinutes { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MobileQuizQuestionDto> Quizzes { get; set; } = new();
    public MobileTutorialSummaryDto? PreviousTutorial { get; set; }
    public MobileTutorialSummaryDto? NextTutorial { get; set; }
}

public class MobileQuizQuestionDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}
#endregion

#region Progress & Learning DTOs
public class MobileCompleteTutorialRequest
{
    public int TutorialId { get; set; }
}

public class MobileCompleteTutorialResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int XpEarned { get; set; }
    public int TotalXp { get; set; }
    public int StreakDays { get; set; }
}
#endregion

#region Curriculum & Roadmap DTOs
public class MobileRoadmapModuleDto
{
    public int ModuleNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public List<MobileTutorialSummaryDto> Lessons { get; set; } = new();
}

public class MobileStreakSummaryDto
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public bool IsActiveToday { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public List<bool> Last7DaysActive { get; set; } = new();
}
#endregion
