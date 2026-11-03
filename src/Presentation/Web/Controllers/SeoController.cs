using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class SeoController : Controller
{
    private readonly AppDbContext _db;

    public SeoController(AppDbContext db)
    {
        _db = db;
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

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> SitemapXml()
    {
        var baseUrl = SeoMetadataService.GetBaseUrl(HttpContext);
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

        // 1. Các trang tĩnh cốt lõi (Song ngữ VI / EN)
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

        // 2. Danh mục bài học (Categories)
        var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.OrderIndex).ToListAsync();
        foreach (var cat in categories)
        {
            var viSlug = cat.GetSlug(false);
            var enSlug = cat.GetSlug(true);
            AddBilingualEntry($"/Tutorials?category={viSlug}", $"/Tutorials?category={enSlug}&culture=en", "0.8", "weekly");
        }

        // 3. Toàn bộ bài học đã xuất bản (Tutorials: /bai-hoc/{slug} & /lessons/{enSlug})
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

        // 4. Toàn bộ đồ án thực chiến (Capstone Projects: /do-an/{slug} & /projects/{enSlug})
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

        return Content(sw.ToString(), "application/xml", Encoding.UTF8);
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

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
