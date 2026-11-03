using System.Net;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class SeoController : Controller
{
    public const string IndexNowKey = "nettutos-indexnow-2026";
    private readonly AppDbContext _db;
    private readonly IAppCacheService? _cache;

    public SeoController(AppDbContext db, IAppCacheService? cache = null)
    {
        _db = db;
        _cache = cache;
    }

    [HttpGet("/api/cache/diagnostics")]
    public async Task<IActionResult> CacheDiagnostics()
    {
        if (_cache == null)
        {
            return Ok(new { activeProvider = "None" });
        }
        var snapshot = await _cache.GetDiagnosticsAsync();
        return Ok(snapshot);
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult RobotsTxt()
    {
        var baseUrl = SeoMetadataService.GetBaseUrl(HttpContext);
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Allow: /bai-hoc/");
        sb.AppendLine("Allow: /lessons/");
        sb.AppendLine("Allow: /do-an/");
        sb.AppendLine("Allow: /projects/");
        sb.AppendLine("Allow: /seo/og-image/");
        sb.AppendLine("Allow: /seo/og-project/");
        sb.AppendLine("Allow: /Tutorials");
        sb.AppendLine("Allow: /Curriculum");
        sb.AppendLine("Allow: /Roadmap");
        sb.AppendLine("Allow: /Playground");
        sb.AppendLine("Allow: /Interview");
        sb.AppendLine("Allow: /AiTutor");
        sb.AppendLine("Disallow: /Admin/");
        sb.AppendLine("Disallow: /Account/");
        sb.AppendLine("Disallow: /Dashboard/");
        sb.AppendLine("Disallow: /api/");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }

    [HttpGet("/indexnow-key.txt")]
    [HttpGet("/nettutos-indexnow-2026.txt")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult IndexNowKeyTxt()
    {
        return Content(IndexNowKey, "text/plain", Encoding.UTF8);
    }

    [HttpGet("/api/seo/diagnostics")]
    public async Task<IActionResult> Diagnostics()
    {
        var baseUrl = SeoMetadataService.GetBaseUrl(HttpContext);
        bool isEn = HttpContext.IsEnglish();
        string country = HttpContext.GetDetectedCountry();
        int tutorialsCount = await _db.Tutorials.AsNoTracking().CountAsync();
        int projectsCount = await _db.CapstoneProjects.AsNoTracking().Where(p => p.IsPublished).CountAsync();
        int categoriesCount = await _db.Categories.AsNoTracking().CountAsync();

        return Ok(new
        {
            status = "Healthy",
            geoIp = new
            {
                detectedCountry = country,
                isVietnamRegion = country.Equals("VN", StringComparison.OrdinalIgnoreCase),
                resolvedLanguage = isEn ? "en" : "vi",
                contentLanguageHeader = isEn ? "en-US" : "vi-VN"
            },
            seo = new
            {
                baseUrl,
                sitemapUrl = $"{baseUrl}/sitemap.xml",
                robotsUrl = $"{baseUrl}/robots.txt",
                indexNowKeyUrl = $"{baseUrl}/{IndexNowKey}.txt",
                dynamicOgTutorialTemplate = $"{baseUrl}/seo/og-image/{{slug}}.svg?lang={{vi|en}}",
                dynamicOgProjectTemplate = $"{baseUrl}/seo/og-project/{{slug}}.svg?lang={{vi|en}}",
                indexedTutorials = tutorialsCount,
                indexedProjects = projectsCount,
                indexedCategories = categoriesCount,
                supportedHreflang = new[] { "vi", "en", "x-default" },
                schemaOrgTypes = new[] { "EducationalOrganization", "WebSite", "BreadcrumbList", "TechArticle", "FAQPage", "Course" }
            }
        });
    }

    [HttpGet("/seo/og-image/{slug}.svg")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> TutorialOgImage(string slug, [FromQuery] string? lang = null)
    {
        bool isEn = !string.IsNullOrEmpty(lang)
            ? lang.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            : HttpContext.IsEnglish();

        string canonicalSlug = LocalizationHelper.GetCanonicalSlug(slug);
        var tutorial = await _db.Tutorials
            .AsNoTracking()
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Slug == canonicalSlug || t.Slug == slug);

        string title = tutorial != null
            ? tutorial.GetTitle(isEn)
            : FormatFallbackSlugTitle(slug);

        string summary = tutorial != null
            ? tutorial.GetSummary(isEn)
            : (isEn ? "Interactive .NET 10 & C# 14 Engineering Lesson on NET-Tutos" : "Bài giảng kỹ thuật .NET 10 & C# 14 thực chiến tại NET-Tutos");

        string categoryBadge = tutorial?.Category != null
            ? tutorial.Category.GetTitle(isEn)
            : (isEn ? ".NET 10 & C# 14" : "Lập trình .NET 10");

        string levelBadge = tutorial != null
            ? LocalizationHelper.GetDifficultyName(tutorial.Difficulty, isEn)
            : (isEn ? "All Levels" : "Mọi cấp độ");

        int minutes = tutorial?.EstimatedReadingMinutes ?? 10;
        string footerMeta = isEn
            ? $"{minutes} min read • Interactive C# Playground • AI Tutor"
            : $"{minutes} phút đọc • Chạy code C# trực tiếp • AI Gia sư";

        var svg = RenderDynamicOgSvg(title, summary, categoryBadge, levelBadge, footerMeta, isEn ? "EN" : "VI");
        return Content(svg, "image/svg+xml", Encoding.UTF8);
    }

    [HttpGet("/seo/og-project/{slug}.svg")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> ProjectOgImage(string slug, [FromQuery] string? lang = null)
    {
        bool isEn = !string.IsNullOrEmpty(lang)
            ? lang.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            : HttpContext.IsEnglish();

        string canonicalSlug = LocalizationHelper.GetCanonicalSlug(slug);
        var project = await _db.CapstoneProjects
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == canonicalSlug || p.Slug == slug);

        string title = project != null
            ? project.GetProjectTitle(isEn)
            : FormatFallbackSlugTitle(slug);

        string summary = project != null
            ? project.GetProjectShortDesc(isEn)
            : (isEn ? "Enterprise .NET 10 Capstone Project with Architecture Blueprint" : "Đồ án thực chiến .NET 10 chuẩn kiến trúc doanh nghiệp");

        string categoryBadge = isEn ? "CAPSTONE PROJECT" : "ĐỒ ÁN THỰC CHIẾN";
        string levelBadge = project != null
            ? LocalizationHelper.GetDifficultyName(project.Level, isEn)
            : (isEn ? "Intermediate" : "Trung cấp");

        int hours = project?.EstimatedHours ?? 15;
        int xp = project?.RewardXp ?? 200;
        string footerMeta = isEn
            ? $"{hours}h Engineering Build • +{xp} XP • Portfolio Verified"
            : $"{hours} giờ thực hành • +{xp} XP • Chứng nhận Hồ sơ Năng lực";

        var svg = RenderDynamicOgSvg(title, summary, categoryBadge, levelBadge, footerMeta, isEn ? "EN" : "VI");
        return Content(svg, "image/svg+xml", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> SitemapXml()
    {
        var baseUrl = SeoMetadataService.GetBaseUrl(HttpContext);
        string cacheKey = $"seo:sitemap:{baseUrl}";
        if (_cache != null)
        {
            var cachedXml = await _cache.GetAsync<string>(cacheKey);
            if (!string.IsNullOrEmpty(cachedXml))
            {
                return Content(cachedXml, "application/xml", Encoding.UTF8);
            }
        }
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XNamespace xhtml = "http://www.w3.org/1999/xhtml";

        var urlset = new XElement(ns + "urlset",
            new XAttribute(XNamespace.Xmlns + "xhtml", xhtml.NamespaceName));

        void AddBilingualEntry(string viPath, string enPath, string priority, string changeFreq, DateTime? lastMod = null)
        {
            string viUrl = $"{baseUrl}{viPath}";
            string enUrl = $"{baseUrl}{enPath}";
            string modStr = (lastMod ?? DateTime.UtcNow).ToString("yyyy-MM-dd");

            foreach (var loc in new[] { viUrl, enUrl }.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                urlset.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", loc),
                    new XElement(ns + "lastmod", modStr),
                    new XElement(ns + "changefreq", changeFreq),
                    new XElement(ns + "priority", priority),
                    new XElement(xhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "vi"),
                        new XAttribute("href", viUrl)),
                    new XElement(xhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "en"),
                        new XAttribute("href", enUrl)),
                    new XElement(xhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "x-default"),
                        new XAttribute("href", enUrl))
                ));
            }
        }

        // 1. Core static routes (Bilingual VI / EN)
        AddBilingualEntry("/?culture=vi", "/?culture=en", "1.0", "daily");
        AddBilingualEntry("/Tutorials?culture=vi", "/Tutorials?culture=en", "0.9", "daily");
        AddBilingualEntry("/chuong-trinh-csharp-toan-dien", "/csharp-full-curriculum", "0.9", "weekly");
        AddBilingualEntry("/Roadmap?culture=vi", "/Roadmap?culture=en", "0.9", "weekly");
        AddBilingualEntry("/Playground?culture=vi", "/Playground?culture=en", "0.8", "weekly");
        AddBilingualEntry("/Project?culture=vi", "/Project?culture=en", "0.8", "weekly");
        AddBilingualEntry("/phong-van-csharp", "/interview-prep", "0.8", "weekly");
        AddBilingualEntry("/AiTutor?culture=vi", "/AiTutor?culture=en", "0.8", "weekly");
        AddBilingualEntry("/Leaderboard?culture=vi", "/Leaderboard?culture=en", "0.7", "daily");
        AddBilingualEntry("/ke-hoach-hoc-tap", "/study-planner", "0.7", "weekly");

        // 2. Tutorial categories
        var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.OrderIndex).ToListAsync();
        foreach (var cat in categories)
        {
            var viSlug = cat.GetSlug(false);
            var enSlug = cat.GetSlug(true);
            AddBilingualEntry($"/Tutorials?category={viSlug}", $"/Tutorials?category={enSlug}&culture=en", "0.8", "weekly");
        }

        // 3. All tutorials (/bai-hoc/{slug} & /lessons/{enSlug})
        var tutorials = await _db.Tutorials
            .AsNoTracking()
            .OrderBy(t => t.OrderIndex)
            .ToListAsync();

        foreach (var tut in tutorials)
        {
            var viSlug = tut.GetSlug(false);
            var enSlug = tut.GetSlug(true);
            AddBilingualEntry($"/bai-hoc/{viSlug}", $"/lessons/{enSlug}", "0.8", "weekly", tut.UpdatedAt);
        }

        // 4. All published Capstone Projects (/do-an/{slug} & /projects/{enSlug})
        var projects = await _db.CapstoneProjects
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderBy(p => p.OrderIndex)
            .ToListAsync();

        foreach (var proj in projects)
        {
            var viSlug = proj.GetSlug(false);
            var enSlug = proj.GetSlug(true);
            AddBilingualEntry($"/do-an/{viSlug}", $"/projects/{enSlug}", "0.8", "weekly", proj.CreatedAt);
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
        using var sw = new Utf8StringWriter();
        doc.Save(sw);

        var xmlString = sw.ToString();
        if (_cache != null)
        {
            await _cache.SetAsync(cacheKey, xmlString, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(5));
        }
        return Content(xmlString, "application/xml", Encoding.UTF8);
    }

    [HttpGet("/manifest.webmanifest")]
    [HttpGet("/site.webmanifest")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult WebManifest()
    {
        bool isEn = HttpContext.IsEnglish();
        var manifest = new
        {
            name = isEn
                ? "NET-Tutos — Modern .NET 10 & C# Engineering Platform"
                : "NET-Tutos — Hệ Sinh Thái Học .NET 10 & C# Thực Chiến",
            short_name = "NET-Tutos",
            description = isEn
                ? "Interactive .NET 10, C# 13, ASP.NET Core, EF Core & Microservices learning platform."
                : "Nền tảng học lập trình .NET 10, C# 13, ASP.NET Core, EF Core & Microservices tương tác trực quan.",
            start_url = "/",
            display = "standalone",
            background_color = "#0f172a",
            theme_color = "#6366f1",
            lang = isEn ? "en" : "vi",
            icons = new[]
            {
                new { src = "/images/og-banner.svg", sizes = "any", type = "image/svg+xml" }
            }
        };

        return Json(manifest);
    }

    private static string RenderDynamicOgSvg(
        string title,
        string summary,
        string categoryBadge,
        string levelBadge,
        string footerMeta,
        string langCode)
    {
        var titleLines = WrapText(title, maxCharsPerLine: 36, maxLines: 2);
        var summaryLines = WrapText(summary, maxCharsPerLine: 68, maxLines: 2);

        string safeCat = WebUtility.HtmlEncode(categoryBadge);
        string safeLevel = WebUtility.HtmlEncode(levelBadge);
        string safeFooter = WebUtility.HtmlEncode(footerMeta);
        string safeLang = WebUtility.HtmlEncode(langCode);

        var sb = new StringBuilder();
        sb.AppendLine("""<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">""");
        sb.AppendLine("""  <defs>""");
        sb.AppendLine("""    <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">""");
        sb.AppendLine("""      <stop offset="0%" stop-color="#0f172a"/>""");
        sb.AppendLine("""      <stop offset="55%" stop-color="#1e1b4b"/>""");
        sb.AppendLine("""      <stop offset="100%" stop-color="#312e81"/>""");
        sb.AppendLine("""    </linearGradient>""");
        sb.AppendLine("""    <linearGradient id="accent" x1="0%" y1="0%" x2="100%" y2="0%">""");
        sb.AppendLine("""      <stop offset="0%" stop-color="#6366f1"/>""");
        sb.AppendLine("""      <stop offset="100%" stop-color="#22d3ee"/>""");
        sb.AppendLine("""    </linearGradient>""");
        sb.AppendLine("""  </defs>""");
        sb.AppendLine("""  <rect width="1200" height="630" fill="url(#bg)"/>""");
        sb.AppendLine("""  <rect x="52" y="52" width="1096" height="526" rx="28" fill="#0f172a" fill-opacity="0.65" stroke="#6366f1" stroke-opacity="0.45" stroke-width="2"/>""");

        // Top Badges
        sb.AppendLine($"""  <rect x="100" y="105" width="320" height="44" rx="22" fill="#4f46e5" fill-opacity="0.28" stroke="#818cf8" stroke-width="1.5"/>""");
        sb.AppendLine($"""  <text x="260" y="133" fill="#c7d2fe" font-family="Inter, Segoe UI, system-ui, sans-serif" font-size="18" font-weight="700" text-anchor="middle">{safeCat}</text>""");

        sb.AppendLine($"""  <rect x="440" y="105" width="180" height="44" rx="22" fill="#0ea5e9" fill-opacity="0.2" stroke="#38bdf8" stroke-width="1.5"/>""");
        sb.AppendLine($"""  <text x="530" y="133" fill="#bae6fd" font-family="Inter, Segoe UI, system-ui, sans-serif" font-size="18" font-weight="700" text-anchor="middle">{safeLevel}</text>""");

        sb.AppendLine($"""  <rect x="1020" y="105" width="80" height="44" rx="14" fill="#1e293b" stroke="#475569" stroke-width="1.5"/>""");
        sb.AppendLine($"""  <text x="1060" y="133" fill="#f8fafc" font-family="Inter, Segoe UI, system-ui, sans-serif" font-size="18" font-weight="800" text-anchor="middle">{safeLang}</text>""");

        // Title lines
        int titleY = 235;
        foreach (var line in titleLines)
        {
            sb.AppendLine($"""  <text x="100" y="{titleY}" fill="#ffffff" font-family="Inter, Segoe UI, system-ui, sans-serif" font-size="48" font-weight="800">{WebUtility.HtmlEncode(line)}</text>""");
            titleY += 62;
        }

        // Summary lines
        int summaryY = Math.Max(375, titleY + 12);
        foreach (var line in summaryLines)
        {
            sb.AppendLine($"""  <text x="100" y="{summaryY}" fill="#cbd5e1" font-family="Inter, Segoe UI, system-ui, sans-serif" font-size="24" font-weight="400">{WebUtility.HtmlEncode(line)}</text>""");
            summaryY += 36;
        }

        // Accent bar & Footer
        sb.AppendLine("""  <rect x="100" y="472" width="1000" height="3" rx="1.5" fill="url(#accent)" fill-opacity="0.6"/>""");
        sb.AppendLine($"""  <text x="100" y="525" fill="#94a3b8" font-family="Inter, Segoe UI, system-ui, sans-serif" font-size="22" font-weight="600">{safeFooter}</text>""");
        sb.AppendLine("""  <text x="1100" y="525" fill="#818cf8" font-family="Inter, Segoe UI, system-ui, sans-serif" font-size="24" font-weight="800" text-anchor="end">NET-Tutos</text>""");
        sb.AppendLine("""</svg>""");

        return sb.ToString();
    }

    private static List<string> WrapText(string text, int maxCharsPerLine, int maxLines)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            result.Add("NET-Tutos");
            return result;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();

        foreach (var word in words)
        {
            if (current.Length + word.Length + 1 <= maxCharsPerLine)
            {
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }
            else
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }

                if (result.Count == maxLines - 1)
                {
                    current.Append(word);
                }
                else
                {
                    current.Append(word);
                }
            }
        }

        if (current.Length > 0 && result.Count < maxLines)
        {
            var line = current.ToString();
            if (line.Length > maxCharsPerLine)
            {
                line = string.Concat(line.AsSpan(0, maxCharsPerLine - 1).TrimEnd(), "…");
            }
            result.Add(line);
        }

        return result;
    }

    private static string FormatFallbackSlugTitle(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return "NET-Tutos Engineering";
        return string.Join(' ', slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
