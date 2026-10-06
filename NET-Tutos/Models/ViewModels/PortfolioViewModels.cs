using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class PublicPortfolioViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public DeveloperProfile Profile { get; set; } = null!;

    public string Headline { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string LevelTitle { get; set; } = string.Empty;
    public int CurrentLevel { get; set; } = 1;
    public int OverallRank { get; set; } = 1;
    public int TotalLearners { get; set; } = 1;

    public int TotalLessonsCompleted { get; set; }
    public int TotalChallengesSolved { get; set; }
    public int TotalQuizzesPassed { get; set; }
    public int TotalProjectsApproved { get; set; }
    public int TotalCertificatesEarned { get; set; }

    public List<PortfolioCertificateItem> Certificates { get; set; } = new();
    public List<PortfolioProjectItem> Projects { get; set; } = new();
    public List<PortfolioBadgeItem> Badges { get; set; } = new();
    public List<PortfolioChallengeItem> SolvedChallenges { get; set; } = new();
    public List<SkillCompetencyItem> SkillMatrix { get; set; } = new();

    public bool IsOwner { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class PortfolioCertificateItem
{
    public int Id { get; set; }
    public string CertificateCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string StudentFullName { get; set; } = string.Empty;
    public double FinalScore { get; set; }
    public DateTime IssuedAt { get; set; }
    public string VerificationUrl { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
}

public class PortfolioProjectItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string GitHubRepoUrl { get; set; } = string.Empty;
    public string? LiveDemoUrl { get; set; }
    public int? Score { get; set; }
    public string? ReviewerFeedback { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<string> TechTags { get; set; } = new();
}

public class PortfolioBadgeItem
{
    public string BadgeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-award";
    public string ColorClass { get; set; } = "primary";
    public DateTime EarnedAt { get; set; }
}

public class PortfolioChallengeItem
{
    public int ChallengeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime SolvedAt { get; set; }
}

public class SkillCompetencyItem
{
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ProficiencyPercentage { get; set; }
    public string MasteryLevel { get; set; } = string.Empty; // e.g. "Thành thạo (Advanced)", "Nền tảng (Fundamental)"
    public string IconClass { get; set; } = "bi-code-square";
    public string ColorClass { get; set; } = "primary";
}

public class UpdateDeveloperProfileRequest
{
    public string? Headline { get; set; }
    public string? CustomBio { get; set; }
    public string? GithubUrl { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Location { get; set; }
    public string? SkillsCsv { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool ShowEmail { get; set; } = true;
}
