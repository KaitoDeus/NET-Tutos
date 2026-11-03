using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Services;

public class SeoPageMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string AlternateViUrl { get; set; } = string.Empty;
    public string AlternateEnUrl { get; set; } = string.Empty;
    public string Robots { get; set; } = "index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1";
    public string OgType { get; set; } = "website";
    public string OgLocale { get; set; } = "vi_VN";
    public string OgLocaleAlternate { get; set; } = "en_US";
    public string OgImage { get; set; } = string.Empty;
    public string SiteName { get; set; } = "NET-Tutos";
    public string DetectedCountry { get; set; } = "VN";
    public string LanguageCode { get; set; } = "vi";
    public string JsonLd { get; set; } = string.Empty;
}

public interface ISeoMetadataService
{
    SeoPageMetadata BuildMetadata(HttpContext context, IDictionary<string, object?>? viewData = null);
}

public partial class SeoMetadataService : ISeoMetadataService
{
    private static readonly JsonSerializerOptions JsonLdOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public SeoPageMetadata BuildMetadata(HttpContext context, IDictionary<string, object?>? viewData = null)
    {
        bool isEn = context.IsEnglish();
        string country = context.GetDetectedCountry();
        string baseUrl = GetBaseUrl(context);

        var routeValues = context.Request.RouteValues;
        string controller = (routeValues["controller"]?.ToString() ?? "Home").Trim();
        string action = (routeValues["action"]?.ToString() ?? "Index").Trim();
        string? slug = routeValues["slug"]?.ToString();
        string? id = routeValues["id"]?.ToString();

        var tutorial = viewData != null && viewData.TryGetValue("SeoTutorial", out var tutObj) ? tutObj as Tutorial : null;
        var project = viewData != null && viewData.TryGetValue("SeoProject", out var projObj) ? projObj as CapstoneProject : null;

        string rawViewTitle = viewData != null && viewData.TryGetValue("Title", out var titleObj)
            ? titleObj?.ToString()?.Trim() ?? string.Empty
            : string.Empty;

        string customDesc = viewData != null && viewData.TryGetValue("MetaDescription", out var descObj)
            ? descObj?.ToString()?.Trim() ?? string.Empty
            : string.Empty;

        string customKeywords = viewData != null && viewData.TryGetValue("MetaKeywords", out var kwObj)
            ? kwObj?.ToString()?.Trim() ?? string.Empty
            : string.Empty;

        var (viPath, enPath) = ResolveBilingualPaths(context, controller, action, slug, id, tutorial, project);

        string canonicalPath = isEn ? enPath : viPath;
        string canonicalUrl = $"{baseUrl}{canonicalPath}";
        string alternateViUrl = $"{baseUrl}{viPath}";
        string alternateEnUrl = $"{baseUrl}{enPath}";

        string brandSuffix = isEn
            ? "NET-Tutos — Interactive .NET 10 & C# Engineering Platform"
            : "NET-Tutos — Hệ Sinh Thái Học Lập Trình .NET 10 & C# Thực Chiến";

        string fullTitle = BuildFullTitle(rawViewTitle, controller, action, isEn, brandSuffix, tutorial, project);
        string description = !string.IsNullOrWhiteSpace(customDesc)
            ? TruncateClean(customDesc, 165)
            : BuildDefaultDescription(controller, action, isEn, tutorial, project, rawViewTitle);

        string keywords = !string.IsNullOrWhiteSpace(customKeywords)
            ? customKeywords
            : BuildDefaultKeywords(controller, isEn, tutorial, project);

        bool isNoIndex = controller.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                      || controller.Equals("Account", StringComparison.OrdinalIgnoreCase)
                      || controller.Equals("Dashboard", StringComparison.OrdinalIgnoreCase)
                      || action.Equals("Error", StringComparison.OrdinalIgnoreCase);

        string robots = isNoIndex
            ? "noindex, nofollow"
            : "index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1";

        bool isTutorialDetail = tutorial != null ||
            (controller.Equals("Tutorials", StringComparison.OrdinalIgnoreCase) && action.Equals("Details", StringComparison.OrdinalIgnoreCase));
        bool isProjectDetail = project != null ||
            (controller.Equals("Project", StringComparison.OrdinalIgnoreCase) && action.Equals("Details", StringComparison.OrdinalIgnoreCase));

        string ogType = (isTutorialDetail || isProjectDetail) ? "article" : "website";

        string langParam = isEn ? "en" : "vi";
        string ogImage = $"{baseUrl}/images/og-banner.svg";
        if (isTutorialDetail)
        {
            string activeSlug = tutorial?.GetSlug(isEn) ?? slug ?? "dotnet-tutorial";
            ogImage = $"{baseUrl}/seo/og-image/{Uri.EscapeDataString(activeSlug)}.svg?lang={langParam}";
        }
        else if (isProjectDetail)
        {
            string activeSlug = project?.GetSlug(isEn) ?? slug ?? "dotnet-project";
            ogImage = $"{baseUrl}/seo/og-project/{Uri.EscapeDataString(activeSlug)}.svg?lang={langParam}";
        }

        string jsonLd = BuildJsonLd(baseUrl, canonicalUrl, fullTitle, description, isEn, controller, action, tutorial, project, ogImage);

        return new SeoPageMetadata
        {
            Title = fullTitle,
            Description = description,
            Keywords = keywords,
            CanonicalUrl = canonicalUrl,
            AlternateViUrl = alternateViUrl,
            AlternateEnUrl = alternateEnUrl,
            Robots = robots,
            OgType = ogType,
            OgLocale = isEn ? "en_US" : "vi_VN",
            OgLocaleAlternate = isEn ? "vi_VN" : "en_US",
            OgImage = ogImage,
            SiteName = "NET-Tutos",
            DetectedCountry = country,
            LanguageCode = langParam,
            JsonLd = jsonLd
        };
    }

    public static string GetBaseUrl(HttpContext context)
    {
        var configuredBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");
        if (!string.IsNullOrWhiteSpace(configuredBase))
        {
            return configuredBase.TrimEnd('/');
        }

        var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) && !string.IsNullOrWhiteSpace(proto)
            ? proto.ToString().Split(',')[0].Trim()
            : context.Request.Scheme;

        var host = context.Request.Host.HasValue ? context.Request.Host.Value : "localhost:5000";
        return $"{scheme}://{host}";
    }

    private static (string ViPath, string EnPath) ResolveBilingualPaths(
        HttpContext context,
        string controller,
        string action,
        string? slug,
        string? id,
        Tutorial? tutorial,
        CapstoneProject? project)
    {
        if (controller.Equals("Home", StringComparison.OrdinalIgnoreCase) && action.Equals("Index", StringComparison.OrdinalIgnoreCase))
        {
            return ("/?culture=vi", "/?culture=en");
        }

        if (controller.Equals("Tutorials", StringComparison.OrdinalIgnoreCase) && action.Equals("Details", StringComparison.OrdinalIgnoreCase))
        {
            string rawSlug = tutorial?.Slug ?? slug ?? string.Empty;
            string viSlug = LocalizationHelper.GetCanonicalSlug(rawSlug);
            string enSlug = tutorial != null
                ? tutorial.GetSlug(true)
                : (!string.IsNullOrEmpty(viSlug) ? TranslateTutorialSlugToEn(viSlug) : rawSlug);

            return ($"/bai-hoc/{viSlug}", $"/lessons/{enSlug}");
        }

        if (controller.Equals("Project", StringComparison.OrdinalIgnoreCase) && action.Equals("Details", StringComparison.OrdinalIgnoreCase))
        {
            string rawSlug = project?.Slug ?? slug ?? string.Empty;
            string viSlug = LocalizationHelper.GetCanonicalSlug(rawSlug);
            string enSlug = project != null
                ? project.GetSlug(true)
                : (new CapstoneProject { Slug = viSlug }.GetSlug(true));
            return ($"/do-an/{viSlug}", $"/projects/{enSlug}");
        }

        if (controller.Equals("Curriculum", StringComparison.OrdinalIgnoreCase))
        {
            if (action.Equals("Details", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(id))
            {
                return ($"/chuong-trinh-csharp/{id}", $"/csharp-curriculum/{id}");
            }
            return ("/chuong-trinh-csharp-toan-dien", "/csharp-full-curriculum");
        }

        if (controller.Equals("Interview", StringComparison.OrdinalIgnoreCase) && action.Equals("Index", StringComparison.OrdinalIgnoreCase))
        {
            return ("/phong-van-csharp", "/interview-prep");
        }

        if (controller.Equals("Planner", StringComparison.OrdinalIgnoreCase) && action.Equals("Index", StringComparison.OrdinalIgnoreCase))
        {
            return ("/ke-hoach-hoc-tap", "/study-planner");
        }

        if (controller.Equals("Streak", StringComparison.OrdinalIgnoreCase) && action.Equals("Index", StringComparison.OrdinalIgnoreCase))
        {
            return ("/chuoi-ngay-hoc", "/streak");
        }

        string currentPath = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        return ($"{currentPath}?culture=vi", $"{currentPath}?culture=en");
    }

    private static string TranslateTutorialSlugToEn(string viSlug)
    {
        var dummy = new Tutorial { Slug = viSlug };
        return dummy.GetSlug(true);
    }

    private static string BuildFullTitle(
        string rawViewTitle,
        string controller,
        string action,
        bool isEn,
        string brandSuffix,
        Tutorial? tutorial,
        CapstoneProject? project)
    {
        if (tutorial != null)
        {
            var tutTitle = tutorial.GetTitle(isEn);
            var catTitle = tutorial.Category?.GetTitle(isEn);
            return !string.IsNullOrWhiteSpace(catTitle)
                ? $"{tutTitle} — {catTitle} | NET-Tutos"
                : $"{tutTitle} | NET-Tutos";
        }

        if (project != null)
        {
            var projTitle = project.GetProjectTitle(isEn);
            return isEn
                ? $"{projTitle} — .NET Capstone Project | NET-Tutos"
                : $"{projTitle} — Đồ Án .NET Thực Chiến | NET-Tutos";
        }

        if (controller.Equals("Home", StringComparison.OrdinalIgnoreCase) && action.Equals("Index", StringComparison.OrdinalIgnoreCase))
        {
            return isEn
                ? "NET-Tutos — Modern .NET 10, C# 13, ASP.NET Core & Microservices Learning Platform"
                : "NET-Tutos — Nền Tảng Học Lập Trình .NET 10, C# 13, ASP.NET Core & Microservices Thực Chiến";
        }

        if (!string.IsNullOrWhiteSpace(rawViewTitle))
        {
            if (rawViewTitle.Contains("NET-Tutos", StringComparison.OrdinalIgnoreCase))
            {
                return rawViewTitle;
            }
            return $"{rawViewTitle} | {brandSuffix}";
        }

        return brandSuffix;
    }

    private static string BuildDefaultDescription(
        string controller,
        string action,
        bool isEn,
        Tutorial? tutorial,
        CapstoneProject? project,
        string rawViewTitle)
    {
        if (tutorial != null)
        {
            var summary = tutorial.GetSummary(isEn);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                return TruncateClean(summary, 160);
            }
        }

        if (project != null)
        {
            var shortDesc = project.GetProjectShortDesc(isEn);
            if (!string.IsNullOrWhiteSpace(shortDesc))
            {
                return TruncateClean(shortDesc, 160);
            }
        }

        return controller.ToLowerInvariant() switch
        {
            "home" => isEn
                ? "Master modern .NET 10, C# 13, ASP.NET Core MVC, Web API, EF Core, and Microservices with 101+ interactive tutorials, C# Roslyn Playground, AI Tutor, and real-world Capstone Projects."
                : "Học lập trình .NET 10, C# 13, ASP.NET Core MVC, Web API, Entity Framework Core và Microservices qua 101+ bài giảng chuyên sâu, trình biên dịch C# trực tuyến, AI Tutor và đồ án thực chiến.",
            "tutorials" => isEn
                ? "Explore 101+ structured C# and .NET 10 tutorials from fundamentals, OOP, LINQ, Async/Await to EF Core, ASP.NET Core Web API, Docker, and Microservices architecture."
                : "Khám phá hệ thống 101+ bài học C# và .NET 10 bài bản từ cơ bản, hướng đối tượng OOP, LINQ, Async/Await đến EF Core, ASP.NET Core Web API, Docker và kiến trúc Microservices.",
            "curriculum" => isEn
                ? "Complete 18-module C# & .NET Engineering Curriculum designed to take you from beginner to Senior .NET Software Architect."
                : "Chương trình đào tạo Kỹ sư C# & .NET toàn diện gồm 18 học phần chuyên sâu từ nền tảng đến kiến trúc sư phần mềm .NET Senior.",
            "roadmap" => isEn
                ? "Step-by-step .NET Backend & Fullstack Developer Roadmap with interactive progress tracking, skill milestones, and recommended tutorials."
                : "Lộ trình trở thành Kỹ sư .NET Backend & Fullstack từng bước với theo dõi tiến độ trực quan, cột mốc kỹ năng và bài học khuyến nghị.",
            "playground" => isEn
                ? "Run, compile, and benchmark C# 13 / .NET 10 code directly in your browser with our isolated Roslyn Online Compiler & Code Sandbox."
                : "Chạy, biên dịch và kiểm thử code C# 13 / .NET 10 trực tiếp trên trình duyệt với Roslyn Online Compiler & Sandbox tốc độ cao.",
            "project" => isEn
                ? "Build production-ready .NET Capstone Projects with architecture blueprints, GitHub code review checklists, and portfolio verification."
                : "Xây dựng các Đồ án .NET thực chiến chuẩn doanh nghiệp với bản vẽ kiến trúc, danh sách kiểm tra code GitHub và chứng nhận hồ sơ năng lực.",
            "interview" => isEn
                ? "Prepare for Junior, Middle, and Senior C# / .NET technical interviews with curated flashcards, architecture scenarios, and deep-dive answers."
                : "Luyện phỏng vấn kỹ sư C# / .NET mọi cấp độ (Fresher, Junior, Middle, Senior) với bộ câu hỏi chuyên sâu, tình huống hệ thống và flashcards.",
            "aitutor" => isEn
                ? "Personal 24/7 AI .NET Mentor powered by Gemini: get instant C# code reviews, architecture explanations, debugging help, and adaptive quizzes."
                : "Trợ lý AI Gia sư .NET 24/7: giải đáp chuyên sâu C#, review code chuẩn Clean Architecture, gỡ lỗi tức thì và cá nhân hóa lộ trình học tập.",
            "exam" or "certificate" => isEn
                ? "Take official .NET & C# competency exams and earn verifiable developer certificates for your engineering portfolio."
                : "Tham gia kỳ thi đánh giá năng lực .NET & C# trực tuyến và nhận chứng chỉ kỹ sư có mã xác thực điện tử.",
            _ => isEn
                ? $"{(string.IsNullOrEmpty(rawViewTitle) ? ".NET Engineering" : rawViewTitle)} — Learn modern C# 13, .NET 10, ASP.NET Core, and Microservices interactively on NET-Tutos."
                : $"{(string.IsNullOrEmpty(rawViewTitle) ? "Kỹ thuật phần mềm .NET" : rawViewTitle)} — Học lập trình C# 13, .NET 10, ASP.NET Core và Microservices trực quan tại NET-Tutos."
        };
    }

    private static string BuildDefaultKeywords(
        string controller,
        bool isEn,
        Tutorial? tutorial,
        CapstoneProject? project)
    {
        var tags = new List<string>();

        if (tutorial != null)
        {
            tags.Add(tutorial.GetTitle(isEn));
            if (tutorial.Category != null)
            {
                tags.Add(tutorial.Category.GetTitle(isEn));
            }
        }
        else if (project != null)
        {
            tags.Add(project.GetProjectTitle(isEn));
            if (!string.IsNullOrWhiteSpace(project.TechStack))
            {
                tags.AddRange(project.TechStack.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        if (isEn)
        {
            tags.AddRange([
                ".NET 10", "C# 13", "ASP.NET Core", "Entity Framework Core",
                "Learn C#", ".NET Tutorials", "Microservices .NET", "Clean Architecture",
                "C# Online Compiler", "NET-Tutos"
            ]);
        }
        else
        {
            tags.AddRange([
                "Học lập trình C#", "Tự học .NET 10", "ASP.NET Core MVC", "Web API C#",
                "Entity Framework Core", "Lập trình hướng đối tượng C#", "Microservices .NET",
                "Clean Architecture", "Đồ án .NET", "NET-Tutos"
            ]);
        }

        return string.Join(", ", tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string BuildJsonLd(
        string baseUrl,
        string canonicalUrl,
        string title,
        string description,
        bool isEn,
        string controller,
        string action,
        Tutorial? tutorial,
        CapstoneProject? project,
        string ogImage)
    {
        string inLanguage = isEn ? "en-US" : "vi-VN";

        var organization = new Dictionary<string, object?>
        {
            ["@type"] = "EducationalOrganization",
            ["@id"] = $"{baseUrl}/#organization",
            ["name"] = "NET-Tutos",
            ["url"] = baseUrl,
            ["logo"] = $"{baseUrl}/images/og-banner.svg",
            ["description"] = description
        };

        var website = new Dictionary<string, object?>
        {
            ["@type"] = "WebSite",
            ["@id"] = $"{baseUrl}/#website",
            ["url"] = baseUrl,
            ["name"] = "NET-Tutos",
            ["inLanguage"] = inLanguage,
            ["publisher"] = new Dictionary<string, object?> { ["@id"] = $"{baseUrl}/#organization" },
            ["potentialAction"] = new Dictionary<string, object?>
            {
                ["@type"] = "SearchAction",
                ["target"] = $"{baseUrl}/Tutorials?search={{search_term_string}}",
                ["query-input"] = "required name=search_term_string"
            }
        };

        var graph = new List<object>
        {
            organization,
            website,
            BuildBreadcrumbList(baseUrl, canonicalUrl, isEn, controller, tutorial, project)
        };

        if (tutorial != null)
        {
            var article = new Dictionary<string, object?>
            {
                ["@type"] = "TechArticle",
                ["@id"] = $"{canonicalUrl}#article",
                ["headline"] = tutorial.GetTitle(isEn),
                ["description"] = description,
                ["image"] = ogImage,
                ["url"] = canonicalUrl,
                ["inLanguage"] = inLanguage,
                ["datePublished"] = tutorial.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["dateModified"] = tutorial.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["timeRequired"] = $"PT{Math.Max(5, tutorial.EstimatedReadingMinutes)}M",
                ["proficiencyLevel"] = tutorial.Difficulty.ToString(),
                ["author"] = new Dictionary<string, object?> { ["@id"] = $"{baseUrl}/#organization" },
                ["publisher"] = new Dictionary<string, object?> { ["@id"] = $"{baseUrl}/#organization" },
                ["mainEntityOfPage"] = canonicalUrl
            };
            graph.Add(article);
            graph.Add(BuildTutorialFaqSchema(canonicalUrl, tutorial, isEn, description));
        }
        else if (project != null)
        {
            var course = new Dictionary<string, object?>
            {
                ["@type"] = "Course",
                ["@id"] = $"{canonicalUrl}#course",
                ["name"] = project.GetProjectTitle(isEn),
                ["description"] = description,
                ["image"] = ogImage,
                ["url"] = canonicalUrl,
                ["inLanguage"] = inLanguage,
                ["educationalLevel"] = project.Level.ToString(),
                ["timeRequired"] = $"PT{Math.Max(1, project.EstimatedHours)}H",
                ["provider"] = new Dictionary<string, object?> { ["@id"] = $"{baseUrl}/#organization" }
            };
            graph.Add(course);
        }

        var root = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = graph
        };

        return JsonSerializer.Serialize(root, JsonLdOptions);
    }

    private static Dictionary<string, object?> BuildBreadcrumbList(
        string baseUrl,
        string canonicalUrl,
        bool isEn,
        string controller,
        Tutorial? tutorial,
        CapstoneProject? project)
    {
        var items = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["@type"] = "ListItem",
                ["position"] = 1,
                ["name"] = isEn ? "Home" : "Trang chủ",
                ["item"] = baseUrl
            }
        };

        if (tutorial != null)
        {
            items.Add(new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = 2,
                ["name"] = isEn ? "Tutorials" : "Bài học",
                ["item"] = $"{baseUrl}/Tutorials"
            });

            int nextPos = 3;
            if (tutorial.Category != null)
            {
                items.Add(new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = nextPos++,
                    ["name"] = tutorial.Category.GetTitle(isEn),
                    ["item"] = $"{baseUrl}/Tutorials?category={tutorial.Category.GetSlug(isEn)}"
                });
            }

            items.Add(new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = nextPos,
                ["name"] = tutorial.GetTitle(isEn),
                ["item"] = canonicalUrl
            });
        }
        else if (project != null)
        {
            items.Add(new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = 2,
                ["name"] = isEn ? "Capstone Projects" : "Đồ án thực chiến",
                ["item"] = $"{baseUrl}/Project"
            });
            items.Add(new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = 3,
                ["name"] = project.GetProjectTitle(isEn),
                ["item"] = canonicalUrl
            });
        }
        else if (!controller.Equals("Home", StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = 2,
                ["name"] = controller,
                ["item"] = canonicalUrl
            });
        }

        return new Dictionary<string, object?>
        {
            ["@type"] = "BreadcrumbList",
            ["@id"] = $"{canonicalUrl}#breadcrumb",
            ["itemListElement"] = items
        };
    }

    private static Dictionary<string, object?> BuildTutorialFaqSchema(
        string canonicalUrl,
        Tutorial tutorial,
        bool isEn,
        string description)
    {
        var tutTitle = tutorial.GetTitle(isEn);
        var levelText = LocalizationHelper.GetDifficultyName(tutorial.Difficulty, isEn);
        var faqEntities = new List<Dictionary<string, object?>>();

        if (tutorial.QuizQuestions != null && tutorial.QuizQuestions.Count > 0)
        {
            foreach (var q in tutorial.QuizQuestions.Take(3))
            {
                string correctAnswer = q.CorrectOption.ToUpperInvariant() switch
                {
                    "A" => q.OptionA,
                    "B" => q.OptionB,
                    "C" => q.OptionC,
                    "D" => q.OptionD,
                    _ => q.OptionA
                };
                string answerBody = !string.IsNullOrWhiteSpace(q.Explanation)
                    ? $"{correctAnswer}. {q.Explanation}"
                    : correctAnswer;

                if (!string.IsNullOrWhiteSpace(q.Question) && !string.IsNullOrWhiteSpace(answerBody))
                {
                    faqEntities.Add(new Dictionary<string, object?>
                    {
                        ["@type"] = "Question",
                        ["name"] = q.Question,
                        ["acceptedAnswer"] = new Dictionary<string, object?>
                        {
                            ["@type"] = "Answer",
                            ["text"] = answerBody
                        }
                    });
                }
            }
        }

        faqEntities.Add(new Dictionary<string, object?>
        {
            ["@type"] = "Question",
            ["name"] = isEn
                ? $"What will I learn in \"{tutTitle}\"?"
                : $"Bài học \"{tutTitle}\" trang bị những kiến thức gì?",
            ["acceptedAnswer"] = new Dictionary<string, object?>
            {
                ["@type"] = "Answer",
                ["text"] = description
            }
        });

        faqEntities.Add(new Dictionary<string, object?>
        {
            ["@type"] = "Question",
            ["name"] = isEn
                ? $"What difficulty level and study time are required for \"{tutTitle}\"?"
                : $"Bài học \"{tutTitle}\" thuộc cấp độ nào và mất bao lâu để hoàn thành?",
            ["acceptedAnswer"] = new Dictionary<string, object?>
            {
                ["@type"] = "Answer",
                ["text"] = isEn
                    ? $"This lesson is designed for {levelText} learners and takes approximately {Math.Max(5, tutorial.EstimatedReadingMinutes)} minutes including interactive C# code practice."
                    : $"Bài học được thiết kế ở cấp độ {levelText}, thời lượng đọc và thực hành code C# trực tiếp khoảng {Math.Max(5, tutorial.EstimatedReadingMinutes)} phút."
            }
        });

        return new Dictionary<string, object?>
        {
            ["@type"] = "FAQPage",
            ["@id"] = $"{canonicalUrl}#faq",
            ["mainEntity"] = faqEntities
        };
    }

    private static string TruncateClean(string input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var cleaned = MarkdownStripRegex().Replace(input, " ");
        cleaned = WhitespaceRegex().Replace(cleaned, " ").Trim();
        if (cleaned.Length <= maxLength) return cleaned;
        return string.Concat(cleaned.AsSpan(0, maxLength - 1).TrimEnd(), "…");
    }

    [GeneratedRegex(@"[#*`_~\[\]>|]+")]
    private static partial Regex MarkdownStripRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
