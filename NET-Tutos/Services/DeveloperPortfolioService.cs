using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class DeveloperPortfolioService : IDeveloperPortfolioService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DeveloperPortfolioService(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<DeveloperProfile> GetOrCreateProfileAsync(string userId)
    {
        var profile = await _context.DeveloperProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            profile = new DeveloperProfile
            {
                UserId = userId,
                Headline = ".NET Software Engineer",
                Location = "Việt Nam",
                SkillsCsv = "C# 14, .NET 10, ASP.NET Core, Entity Framework Core, SQL Server, LINQ, Clean Architecture, RESTful API",
                CustomBio = user?.Bio,
                IsPublic = true,
                ShowEmail = false,
                UpdatedAt = DateTime.UtcNow
            };
            _context.DeveloperProfiles.Add(profile);
            await _context.SaveChangesAsync();
        }
        return profile;
    }

    public async Task<bool> UpdateProfileAsync(string userId, UpdateDeveloperProfileRequest request)
    {
        var profile = await GetOrCreateProfileAsync(userId);
        profile.Headline = string.IsNullOrWhiteSpace(request.Headline) ? ".NET Software Engineer" : request.Headline.Trim();
        profile.CustomBio = request.CustomBio?.Trim();
        profile.GithubUrl = NormalizeUrl(request.GithubUrl);
        profile.LinkedinUrl = NormalizeUrl(request.LinkedinUrl);
        profile.WebsiteUrl = NormalizeUrl(request.WebsiteUrl);
        profile.Location = request.Location?.Trim();
        profile.SkillsCsv = request.SkillsCsv?.Trim();
        profile.IsPublic = request.IsPublic;
        profile.ShowEmail = request.ShowEmail;
        profile.UpdatedAt = DateTime.UtcNow;

        // Also sync Bio to user record if provided
        if (!string.IsNullOrWhiteSpace(request.CustomBio))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.Bio = request.CustomBio.Trim();
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PublicPortfolioViewModel?> GetPortfolioByUsernameAsync(string username, string? currentVisitorUserId, bool isEnglish)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;

        var cleanUsername = username.Trim().ToLower();

        // Find user by UserName or normalized email prefix
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName!.ToLower() == cleanUsername ||
                                      u.Email!.ToLower() == cleanUsername ||
                                      u.UserName!.ToLower().StartsWith(cleanUsername + "@"));

        if (user == null) return null;

        var profile = await GetOrCreateProfileAsync(user.Id);
        bool isOwner = !string.IsNullOrEmpty(currentVisitorUserId) && currentVisitorUserId == user.Id;

        if (!profile.IsPublic && !isOwner)
        {
            return new PublicPortfolioViewModel
            {
                User = user,
                Profile = profile,
                IsOwner = false,
                IsPublic = false
            };
        }

        // 1. Certificates
        var rawCertificates = await _context.Certificates
            .Include(c => c.Category)
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

        var certificates = rawCertificates.Select(c => new PortfolioCertificateItem
        {
            Id = c.Id,
            CertificateCode = c.CertificateCode,
            CourseTitle = isEnglish ? LocalizationHelper.TranslateExamTitle(c.CourseTitle, true) : c.CourseTitle,
            StudentFullName = c.StudentFullName,
            FinalScore = c.FinalScore,
            IssuedAt = c.IssuedAt,
            VerificationUrl = c.VerificationUrl,
            CategoryName = c.Category != null ? (isEnglish ? c.Category.GetTitle(true) : c.Category.Name) : null
        }).ToList();

        // 2. Capstone Projects (Approved)
        var projectSubmissions = await _context.ProjectSubmissions
            .Include(ps => ps.Project)
                .ThenInclude(p => p!.Category)
            .Where(ps => ps.UserId == user.Id && ps.Status == ProjectSubmissionStatus.Approved)
            .OrderByDescending(ps => ps.SubmittedAt)
            .ToListAsync();

        var projects = projectSubmissions.Select(ps => new PortfolioProjectItem
        {
            Id = ps.Id,
            Title = isEnglish ? ps.Project.GetProjectTitle(true) : (ps.Project?.Title ?? "Capstone Project"),
            Description = isEnglish ? ps.Project.GetProjectShortDesc(true) : (ps.Project?.ShortDescription ?? string.Empty),
            CategoryName = isEnglish ? (ps.Project?.Category.GetTitle(true) ?? ".NET Full-Stack") : (ps.Project?.Category?.Name ?? ".NET Full-Stack"),
            Difficulty = ps.Project?.Level.ToString() ?? "Advanced",
            GitHubRepoUrl = ps.GitHubRepoUrl,
            LiveDemoUrl = ps.LiveDemoUrl,
            Score = ps.Score,
            ReviewerFeedback = ps.ReviewerFeedback,
            SubmittedAt = ps.SubmittedAt,
            TechTags = GetProjectTechTags(ps.Project?.Title)
        }).ToList();

        // 3. Badges
        var rawBadges = await _context.UserBadges
            .Where(b => b.UserId == user.Id)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();

        var badges = rawBadges.Select(b => new PortfolioBadgeItem
        {
            BadgeCode = b.BadgeCode,
            Title = isEnglish ? LocalizationHelper.GetBadgeTitle(b.BadgeCode, b.Title, true) : b.Title,
            Description = isEnglish ? LocalizationHelper.GetBadgeDescription(b.BadgeCode, b.Description, true) : b.Description,
            IconClass = b.IconClass,
            ColorClass = b.ColorClass,
            EarnedAt = b.EarnedAt
        }).ToList();

        // 4. Distinct Passed Coding Challenges
        var passedSubmissions = await _context.CodeSubmissions
            .Include(cs => cs.CodingChallenge)
            .Where(cs => cs.UserId == user.Id && cs.IsPassed)
            .OrderByDescending(cs => cs.SubmittedAt)
            .ToListAsync();

        var distinctChallenges = passedSubmissions
            .GroupBy(cs => cs.CodingChallengeId)
            .Select(g => g.First())
            .Select(cs => new PortfolioChallengeItem
            {
                ChallengeId = cs.CodingChallengeId,
                Title = isEnglish ? cs.CodingChallenge.GetChallengeTitle(true) : (cs.CodingChallenge?.Title ?? "Thử thách"),
                Difficulty = cs.CodingChallenge?.Difficulty.ToString() ?? "Easy",
                Slug = cs.CodingChallenge?.Slug ?? cs.CodingChallengeId.ToString(),
                SolvedAt = cs.SubmittedAt
            })
            .ToList();

        // 5. Total counts & Leaderboard Rank
        int totalLessonsCompleted = await _context.UserLessonProgresses
            .CountAsync(p => p.UserId == user.Id && p.IsCompleted);

        int totalQuizzesPassed = await _context.QuizAttempts
            .CountAsync(q => q.UserId == user.Id && q.IsPassed);

        int totalLearners = await _context.Users.CountAsync();
        int rank = 1 + await _context.Users.CountAsync(u => u.ExperiencePoints > user.ExperiencePoints);

        // Calculate Level & Title
        int currentLevel = Math.Max(1, (user.ExperiencePoints / 100) + 1);
        string levelTitle = GetLevelTitle(currentLevel, isEnglish);

        // 6. Skill Matrix calculation
        var skillMatrix = CalculateSkillMatrix(totalLessonsCompleted, distinctChallenges.Count, projects.Count, certificates.Count, isEnglish);

        // Dynamic bilingual fallback for default headline
        string headline;
        if (string.IsNullOrWhiteSpace(profile.Headline) ||
            profile.Headline.Equals(".NET Software Engineer", StringComparison.OrdinalIgnoreCase) ||
            profile.Headline.Equals("Kỹ sư phần mềm .NET", StringComparison.OrdinalIgnoreCase))
        {
            headline = isEnglish ? ".NET Software Engineer" : "Kỹ sư phần mềm .NET";
        }
        else
        {
            headline = profile.Headline;
        }

        // Dynamic bilingual fallback for bio
        string bio;
        if (!string.IsNullOrWhiteSpace(profile.CustomBio))
        {
            if (profile.CustomBio.Equals("Học viên trải nghiệm hệ thống NET-Tutos", StringComparison.OrdinalIgnoreCase) ||
                profile.CustomBio.Equals("Learner exploring the NET-Tutos platform", StringComparison.OrdinalIgnoreCase) ||
                profile.CustomBio.Equals("Learner exploring the NET-Tutos learning platform", StringComparison.OrdinalIgnoreCase))
            {
                bio = isEnglish 
                    ? "Learner exploring the NET-Tutos platform and mastering modern C#." 
                    : "Học viên trải nghiệm hệ thống NET-Tutos và rèn luyện kỹ năng C# hiện đại.";
            }
            else
            {
                bio = profile.CustomBio;
            }
        }
        else if (!string.IsNullOrWhiteSpace(user.Bio))
        {
            if (user.Bio.Equals("Học viên trải nghiệm hệ thống NET-Tutos", StringComparison.OrdinalIgnoreCase) ||
                user.Bio.Equals("Learner exploring the NET-Tutos platform", StringComparison.OrdinalIgnoreCase) ||
                user.Bio.Equals("Learner exploring the NET-Tutos learning platform", StringComparison.OrdinalIgnoreCase))
            {
                bio = isEnglish 
                    ? "Learner exploring the NET-Tutos platform and mastering modern C#." 
                    : "Học viên trải nghiệm hệ thống NET-Tutos và rèn luyện kỹ năng C# hiện đại.";
            }
            else
            {
                bio = user.Bio;
            }
        }
        else
        {
            bio = isEnglish 
                ? "Passionate .NET developer continuously mastering modern C# and cloud technologies." 
                : "Lập trình viên .NET nhiệt huyết, không ngừng rèn luyện kỹ năng Modern C# và hệ sinh thái phần mềm.";
        }

        // Keep location localized in-memory for the view
        if (profile.Location != null)
        {
            if (profile.Location.Equals("Việt Nam", StringComparison.OrdinalIgnoreCase) ||
                profile.Location.Equals("Vietnam", StringComparison.OrdinalIgnoreCase))
            {
                profile.Location = isEnglish ? "Vietnam" : "Việt Nam";
            }
        }

        return new PublicPortfolioViewModel
        {
            User = user,
            Profile = profile,
            Headline = headline,
            Bio = bio,
            LevelTitle = levelTitle,
            CurrentLevel = currentLevel,
            OverallRank = rank,
            TotalLearners = Math.Max(1, totalLearners),
            TotalLessonsCompleted = totalLessonsCompleted,
            TotalChallengesSolved = distinctChallenges.Count,
            TotalQuizzesPassed = totalQuizzesPassed,
            TotalProjectsApproved = projects.Count,
            TotalCertificatesEarned = certificates.Count,
            Certificates = certificates,
            Projects = projects,
            Badges = badges,
            SolvedChallenges = distinctChallenges,
            SkillMatrix = skillMatrix,
            IsOwner = isOwner,
            IsPublic = profile.IsPublic
        };
    }

    private static string GetLevelTitle(int level, bool isEnglish) => level switch
    {
        >= 15 => isEnglish ? "Principal .NET Architect" : "Kiến trúc sư trưởng .NET",
        >= 10 => isEnglish ? "Senior .NET Engineer" : "Kỹ sư .NET Cao cấp (Senior)",
        >= 7  => isEnglish ? "Mid-Level C# Developer" : "Lập trình viên C# Chuyên nghiệp",
        >= 4  => isEnglish ? "Junior .NET Developer" : "Lập trình viên .NET Tiềm năng",
        >= 2  => isEnglish ? "Aspiring .NET Coder" : "Tân binh Lập trình .NET",
        _     => isEnglish ? ".NET Enthusiast" : "Học viên Khởi đầu"
    };

    private static List<SkillCompetencyItem> CalculateSkillMatrix(int lessons, int challenges, int projects, int certs, bool isEn)
    {
        // Dynamic scoring between 40% and 98%
        int csharpScore = Math.Clamp(50 + (lessons * 3) + (challenges * 4), 45, 98);
        int linqScore = Math.Clamp(40 + (challenges * 6) + (lessons * 2), 40, 95);
        int webApiScore = Math.Clamp(35 + (projects * 25) + (lessons * 2), 35, 96);
        int efCoreScore = Math.Clamp(40 + (projects * 20) + (lessons * 2), 40, 95);
        int asyncScore = Math.Clamp(45 + (lessons * 3) + (challenges * 3), 45, 92);
        int architectureScore = Math.Clamp(30 + (projects * 30) + (certs * 20), 30, 96);

        string GetMastery(int score) => score switch
        {
            >= 85 => isEn ? "Advanced / Production-ready" : "Thành thạo / Chuẩn dự án",
            >= 70 => isEn ? "Proficient" : "Khá / Vận dụng tốt",
            >= 55 => isEn ? "Intermediate" : "Trung cấp / Nền tảng vững",
            _     => isEn ? "Foundational" : "Cơ bản / Đang tích lũy"
        };

        return new List<SkillCompetencyItem>
        {
            new() { SkillName = isEn ? "C# 14 & Object-Oriented Design" : "C# 14 & Lập trình hướng đối tượng", Category = isEn ? "Core" : "Nền tảng", ProficiencyPercentage = csharpScore, MasteryLevel = GetMastery(csharpScore), IconClass = "bi-filetype-cs", ColorClass = "primary" },
            new() { SkillName = isEn ? "LINQ & Collections Performance" : "LINQ & Tối ưu xử lý Collections", Category = isEn ? "Algorithms" : "Thuật toán", ProficiencyPercentage = linqScore, MasteryLevel = GetMastery(linqScore), IconClass = "bi-lightning-charge", ColorClass = "warning" },
            new() { SkillName = isEn ? "ASP.NET Core Web API & REST" : "ASP.NET Core Web API & RESTful Service", Category = isEn ? "Backend" : "Backend", ProficiencyPercentage = webApiScore, MasteryLevel = GetMastery(webApiScore), IconClass = "bi-hdd-network", ColorClass = "info" },
            new() { SkillName = isEn ? "Entity Framework Core & SQL" : "Entity Framework Core & Cơ sở dữ liệu SQL", Category = isEn ? "Database" : "Cơ sở dữ liệu", ProficiencyPercentage = efCoreScore, MasteryLevel = GetMastery(efCoreScore), IconClass = "bi-database-fill-gear", ColorClass = "success" },
            new() { SkillName = isEn ? "Async / Multithreading & Tasks" : "Lập trình bất đồng bộ Async & Đa luồng", Category = isEn ? "Concurrency" : "Đa luồng", ProficiencyPercentage = asyncScore, MasteryLevel = GetMastery(asyncScore), IconClass = "bi-cpu-fill", ColorClass = "danger" },
            new() { SkillName = isEn ? "Clean Architecture & Design Patterns" : "Kiến trúc Clean Architecture & Design Patterns", Category = isEn ? "Architecture" : "Kiến trúc", ProficiencyPercentage = architectureScore, MasteryLevel = GetMastery(architectureScore), IconClass = "bi-diagram-3-fill", ColorClass = "primary" }
        };
    }

    private static List<string> GetProjectTechTags(string? projectTitle)
    {
        var tags = new List<string> { ".NET 10", "C# 14", "ASP.NET Core" };
        var title = (projectTitle ?? "").ToLower();

        if (title.Contains("clean") || title.Contains("architecture")) tags.Add("Clean Architecture");
        if (title.Contains("api") || title.Contains("rest")) tags.Add("Web API");
        if (title.Contains("sql") || title.Contains("ef") || title.Contains("commerce")) tags.Add("EF Core");
        if (title.Contains("signalr") || title.Contains("chat") || title.Contains("realtime")) tags.Add("SignalR");
        if (title.Contains("jwt") || title.Contains("auth") || title.Contains("identity")) tags.Add("JWT Auth");
        if (title.Contains("docker") || title.Contains("microservice")) tags.Add("Docker");

        return tags;
    }

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + trimmed;
        }
        return trimmed;
    }
}
