using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;
using NET_Tutos.Data;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;
    private readonly IMarkdownService _markdownService;
    private readonly IObservabilityService? _observabilityService;
    private readonly IKafkaProducer? _kafkaProducer;
    private readonly IKafkaStreamAnalyticsStore? _analyticsStore;
    private readonly IAiTutorService? _aiTutorService;

    public AdminController(
        AppDbContext context,
        IMarkdownService markdownService,
        IObservabilityService? observabilityService = null,
        IKafkaProducer? kafkaProducer = null,
        IKafkaStreamAnalyticsStore? analyticsStore = null,
        IAiTutorService? aiTutorService = null)
    {
        _context = context;
        _markdownService = markdownService;
        _observabilityService = observabilityService;
        _kafkaProducer = kafkaProducer;
        _analyticsStore = analyticsStore;
        _aiTutorService = aiTutorService;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalTutorials = await _context.Tutorials.CountAsync();
        var totalCategories = await _context.Categories.CountAsync();
        var totalQuizQuestions = await _context.QuizQuestions.CountAsync();
        var totalExamsTaken = await _context.QuizAttempts.CountAsync();
        var totalCertificatesIssued = await _context.Certificates.CountAsync();

        var passedExams = await _context.QuizAttempts.CountAsync(a => a.IsPassed);
        double passRate = totalExamsTaken > 0
            ? Math.Round(((double)passedExams / totalExamsTaken) * 100.0, 1)
            : 0;

        var topStudents = await _context.Users
            .OrderByDescending(u => u.ExperiencePoints)
            .Take(5)
            .ToListAsync();

        var recentExams = await _context.QuizAttempts
            .Include(a => a.User)
            .Include(a => a.Category)
            .OrderByDescending(a => a.CompletedAt)
            .Take(8)
            .ToListAsync();

        var recentCertificates = await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Category)
            .OrderByDescending(c => c.IssuedAt)
            .Take(6)
            .ToListAsync();

        var viewModel = new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            TotalTutorials = totalTutorials,
            TotalCategories = totalCategories,
            TotalQuizQuestions = totalQuizQuestions,
            TotalExamsTaken = totalExamsTaken,
            TotalCertificatesIssued = totalCertificatesIssued,
            ExamPassRate = passRate,
            TopStudentsByXp = topStudents,
            RecentExams = recentExams,
            RecentCertificates = recentCertificates
        };

        return View(viewModel);
    }

    // GET: /Admin/Telemetry
    [HttpGet]
    public IActionResult Telemetry()
    {
        return View();
    }

    // GET: /Admin/TelemetryData
    [HttpGet]
    public async Task<IActionResult> TelemetryData([FromQuery] bool pingAi = false, CancellationToken ct = default)
    {
        var observability = _observabilityService != null
            ? await _observabilityService.GetDiagnosticsAsync(ct)
            : null;

        var kafkaProducer = _kafkaProducer != null
            ? await _kafkaProducer.GetDiagnosticsAsync()
            : null;

        var kafkaConsumer = _analyticsStore?.GetSnapshot();

        AiTutorStatusResponse? aiStatus = null;
        if (pingAi && _aiTutorService != null)
        {
            aiStatus = await _aiTutorService.CheckStatusAsync();
        }

        return Json(new
        {
            success = true,
            observability,
            kafkaProducer,
            kafkaConsumer,
            aiStatus,
            serverTimestamp = DateTime.UtcNow
        });
    }

    // POST: /Admin/PreviewMarkdown (AJAX endpoint for Live Preview in WYSIWYG Editor)
    [HttpPost]
    public IActionResult PreviewMarkdown([FromBody] MarkdownPreviewRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Markdown))
        {
            return Ok(new { html = string.Empty });
        }

        var html = _markdownService.ToHtml(request.Markdown);
        return Ok(new { html });
    }

    // GET: /Admin/FreezeOverview
    [HttpGet]
    public async Task<IActionResult> FreezeOverview([FromServices] IWebHostEnvironment env)
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalTutorials = await _context.Tutorials.CountAsync();
        var totalCategories = await _context.Categories.CountAsync();
        var totalQuizQuestions = await _context.QuizQuestions.CountAsync();
        var totalChallenges = await _context.CodingChallenges.CountAsync();
        var totalFlashcards = await _context.InterviewFlashcards.CountAsync();

        var repoRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", ".."));
        var distDir = Path.Combine(repoRoot, "dist");

        var artifacts = new List<ArtifactInfo>();
        var candidates = new (string Name, string RelPath, string Platform)[]
        {
            ("Android Signed APK", "mobile/NET-Tutos-v1.0.0-Signed.apk", "Android 5.0+ (ARM64/x86_64)"),
            ("VS Code Atus Extension", "vscode/vscode-atus-0.1.0.vsix", "VS Code 1.90+ (Node 20+)"),
            ("Production Helm Chart", "helm/nettutos-1.0.0.tgz", "Kubernetes 1.28+ (Helm 3.12+)")
        };

        foreach (var c in candidates)
        {
            var fullPath = Path.Combine(distDir, c.RelPath.Replace('/', Path.DirectorySeparatorChar));
            var exists = System.IO.File.Exists(fullPath);
            string sizeStr = "Chưa đóng gói";
            if (exists)
            {
                var len = new FileInfo(fullPath).Length;
                sizeStr = len > 1024 * 1024
                    ? $"{(len / (1024.0 * 1024.0)):F1} MB"
                    : $"{(len / 1024.0):F1} KB";
            }

            artifacts.Add(new ArtifactInfo
            {
                Name = c.Name,
                RelativePath = $"dist/{c.RelPath}",
                SizeDisplay = sizeStr,
                TargetPlatform = c.Platform,
                Exists = exists
            });
        }

        var model = new SystemFreezeOverviewViewModel
        {
            TotalUsers = totalUsers,
            TotalTutorials = totalTutorials,
            TotalCategories = totalCategories,
            TotalQuizQuestions = totalQuizQuestions,
            TotalCodingChallenges = totalChallenges,
            TotalFlashcards = totalFlashcards,
            ReleaseArtifacts = artifacts
        };

        return View(model);
    }

    // GET: /Admin/DownloadSnapshot
    [HttpGet]
    public IActionResult DownloadSnapshot([FromServices] IWebHostEnvironment env)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", ".."));
        var snapshotPath = Path.Combine(repoRoot, "data", "snapshots", "dotnet_tutorials_freeze.db");
        if (!System.IO.File.Exists(snapshotPath))
        {
            snapshotPath = Path.Combine(env.ContentRootPath, "dotnet_tutorials.db");
        }

        if (!System.IO.File.Exists(snapshotPath))
        {
            return NotFound("Không tìm thấy tệp cơ sở dữ liệu snapshot.");
        }

        var stream = System.IO.File.OpenRead(snapshotPath);
        return File(stream, "application/octet-stream", "dotnet_tutorials_freeze.db");
    }

    // GET: /api/admin/system/health-matrix
    [AllowAnonymous]
    [HttpGet("/api/admin/system/health-matrix")]
    public async Task<IActionResult> HealthMatrix([FromServices] IHttpClientFactory clientFactory, CancellationToken ct = default)
    {
        var services = new (string Name, string Url)[]
        {
            ("CodeRunner Microservice", "http://localhost:5002/healthz"),
            ("Gamification Microservice", "http://localhost:5004/healthz"),
            ("AiTutor Microservice", "http://localhost:5006/healthz"),
            ("Catalog Microservice", "http://localhost:5008/healthz"),
            ("YARP Gateway", "http://localhost:5000/healthz")
        };

        var client = clientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(1.5);

        var results = new List<object>();
        foreach (var (name, url) in services)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool isUp = false;
            int statusCode = 0;
            try
            {
                var res = await client.GetAsync(url, ct);
                sw.Stop();
                statusCode = (int)res.StatusCode;
                isUp = res.IsSuccessStatusCode;
            }
            catch
            {
                sw.Stop();
                isUp = false;
            }

            results.Add(new
            {
                name,
                url,
                isUp,
                statusCode,
                latencyMs = sw.ElapsedMilliseconds
            });
        }

        return Json(new
        {
            success = true,
            timestamp = DateTime.UtcNow,
            services = results
        });
    }
}

public class MarkdownPreviewRequest
{
    public string Markdown { get; set; } = string.Empty;
}
