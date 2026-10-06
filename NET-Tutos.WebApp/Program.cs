using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Hubs;
using NET_Tutos.Models;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Localization configuration (Supports Vietnamese and English)
var supportedCultures = new[]
{
    new CultureInfo("vi"),
    new CultureInfo("en")
};
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("vi");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider(),
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// Antiforgery Configuration for AJAX & Fetch requests
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddSingleton<IMarkdownService, MarkdownService>();
builder.Services.AddScoped<ITutorialService, TutorialService>();
builder.Services.AddScoped<ILearningProgressService, LearningProgressService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<ICodeExecutionService, CodeExecutionService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IDiscussionService, DiscussionService>();
builder.Services.AddScoped<IStreakService, StreakService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IActivityFeedService, ActivityFeedService>();
builder.Services.AddScoped<ICapstoneProjectService, CapstoneProjectService>();
builder.Services.AddScoped<IFlashcardService, FlashcardService>();
builder.Services.AddScoped<ICurriculumService, CurriculumService>();
builder.Services.AddScoped<IStudyPlannerService, StudyPlannerService>();
builder.Services.AddScoped<IAiTutorService, AiTutorService>();
builder.Services.AddScoped<IDeveloperPortfolioService, DeveloperPortfolioService>();
builder.Services.AddScoped<IMobileAuthService, MobileAuthService>();

// Determine Database Provider
var configuredProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
var sqlServerConnection = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=DotNetTutorialsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection") 
    ?? "Data Source=dotnet_tutorials.db";

string activeProvider = "SQL Server (LocalDB)";
string activeProviderEn = "SQL Server (LocalDB)";

if (configuredProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    bool isSqlServerAvailable = false;
    try
    {
        var testBuilder = new SqlConnectionStringBuilder(sqlServerConnection)
        {
            ConnectTimeout = 2
        };
        using var testConn = new SqlConnection(testBuilder.ConnectionString);
        testConn.Open();
        isSqlServerAvailable = true;
    }
    catch
    {
        isSqlServerAvailable = false;
    }

    if (isSqlServerAvailable)
    {
        activeProvider = "SQL Server (LocalDB)";
        activeProviderEn = "SQL Server (LocalDB)";
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(sqlServerConnection));
    }
    else
    {
        activeProvider = "SQLite (Chạy tức thì - Sẵn sàng chuyển SQL Server khi cài đặt)";
        activeProviderEn = "SQLite (Instant Run - Ready to switch to SQL Server)";
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(sqliteConnection));
    }
}
else
{
    activeProvider = "SQLite";
    activeProviderEn = "SQLite";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(sqliteConnection));
}

// Register Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
});

builder.Services.AddSingleton(new DatabaseProviderInfo { Name = activeProvider, NameEn = activeProviderEn });

var app = builder.Build();

// Auto-seed database and create tables
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        logger.LogInformation("Đang khởi tạo cơ sở dữ liệu ({Provider})...", activeProvider);
        await DbInitializer.InitializeAsync(context, userManager, roleManager);
        logger.LogInformation("Cơ sở dữ liệu và dữ liệu mẫu đã sẵn sàng!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi khi khởi tạo cơ sở dữ liệu.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var locOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

app.UseRouting();

// Authentication must be before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Bilingual friendly routes for tutorials: /lessons/{slug}, /tutorials/{slug}, /bai-hoc/{slug}
app.MapControllerRoute(
    name: "tutorial-slug-en",
    pattern: "lessons/{slug}",
    defaults: new { controller = "Tutorials", action = "Details" });

app.MapControllerRoute(
    name: "tutorial-slug-tutorials",
    pattern: "tutorials/{slug}",
    defaults: new { controller = "Tutorials", action = "Details" });

app.MapControllerRoute(
    name: "tutorial-slug",
    pattern: "bai-hoc/{slug}",
    defaults: new { controller = "Tutorials", action = "Details" });

// Bilingual friendly routes for capstone projects: /projects/{slug}, /do-an/{slug}
app.MapControllerRoute(
    name: "project-slug-en",
    pattern: "projects/{slug}",
    defaults: new { controller = "Project", action = "Details" });

app.MapControllerRoute(
    name: "project-slug-vi",
    pattern: "do-an/{slug}",
    defaults: new { controller = "Project", action = "Details" });

app.MapControllerRoute(
    name: "interview-vi",
    pattern: "phong-van-csharp",
    defaults: new { controller = "Interview", action = "Index" });

app.MapControllerRoute(
    name: "interview-en",
    pattern: "interview-prep",
    defaults: new { controller = "Interview", action = "Index" });

app.MapControllerRoute(
    name: "curriculum-vi",
    pattern: "chuong-trinh-csharp-toan-dien",
    defaults: new { controller = "Curriculum", action = "Index" });

app.MapControllerRoute(
    name: "curriculum-en",
    pattern: "csharp-full-curriculum",
    defaults: new { controller = "Curriculum", action = "Index" });

app.MapControllerRoute(
    name: "curriculum-detail-vi",
    pattern: "chuong-trinh-csharp/{id:int}",
    defaults: new { controller = "Curriculum", action = "Details" });

app.MapControllerRoute(
    name: "curriculum-detail-en",
    pattern: "csharp-curriculum/{id:int}",
    defaults: new { controller = "Curriculum", action = "Details" });

app.MapControllerRoute(
    name: "planner-vi",
    pattern: "ke-hoach-hoc-tap",
    defaults: new { controller = "Planner", action = "Index" });

app.MapControllerRoute(
    name: "planner-en",
    pattern: "study-planner",
    defaults: new { controller = "Planner", action = "Index" });

// Friendly certificate verify route: /verify-certificate/{code}
app.MapControllerRoute(
    name: "certificate-verify",
    pattern: "verify-certificate/{code}",
    defaults: new { controller = "Certificate", action = "Verify" });

// SignalR Hub endpoints
app.MapHub<DiscussionHub>("/hubs/discussion");
app.MapHub<NotificationHub>("/hubs/notifications");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
