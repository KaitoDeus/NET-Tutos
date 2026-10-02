using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Hubs;
using NET_Tutos.Models;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

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

// Determine Database Provider
var configuredProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
var sqlServerConnection = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=DotNetTutorialsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection") 
    ?? "Data Source=dotnet_tutorials.db";

string activeProvider = "SQL Server (LocalDB)";

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
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(sqlServerConnection));
    }
    else
    {
        activeProvider = "SQLite (Chạy tức thì - Sẵn sàng chuyển SQL Server khi cài đặt)";
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(sqliteConnection));
    }
}
else
{
    activeProvider = "SQLite";
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

builder.Services.AddSingleton(new DatabaseProviderInfo { Name = activeProvider });

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

app.UseRouting();

// Authentication must be before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Custom friendly route for tutorials: /bai-hoc/{slug}
app.MapControllerRoute(
    name: "tutorial-slug",
    pattern: "bai-hoc/{slug}",
    defaults: new { controller = "Tutorials", action = "Details" });

// SignalR Hub endpoints
app.MapHub<DiscussionHub>("/hubs/discussion");
app.MapHub<NotificationHub>("/hubs/notifications");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
