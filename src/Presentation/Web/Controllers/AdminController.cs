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
}

public class MarkdownPreviewRequest
{
    public string Markdown { get; set; } = string.Empty;
}
