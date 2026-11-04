using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Hubs;
using NET_Tutos.Models;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using Npgsql;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

// Enable Npgsql legacy timestamp behavior for seamless DateTime compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Auto-load .env environment file if present (for development outside Docker)
try
{
    var currentDir = Directory.GetCurrentDirectory();
    string? envPath = null;
    while (!string.IsNullOrEmpty(currentDir))
    {
        var candidate = Path.Combine(currentDir, ".env");
        if (File.Exists(candidate))
        {
            envPath = candidate;
            break;
        }
        var parent = Directory.GetParent(currentDir);
        if (parent == null || parent.FullName == currentDir) break;
        currentDir = parent.FullName;
    }

    if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
    {
        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
            var idx = trimmed.IndexOf('=');
            if (idx > 0)
            {
                var key = trimmed.Substring(0, idx).Trim();
                var val = trimmed.Substring(idx + 1).Trim();
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, val);
                }
            }
        }
    }
}
catch
{
    // Ignore .env read error
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection")
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "nettutos:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddSingleton<IAppCacheService, HybridRedisCacheService>();
builder.Services.AddSingleton<IObservabilityService, ObservabilityService>();
builder.Services.AddTransient<ResilientMicroserviceHandler>();

// CORS configuration for Web MVC & Mobile (.NET MAUI) clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "http://127.0.0.1:5000",
                "https://localhost:5001"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Swagger / OpenAPI specification & Interactive UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = ".NET Tutos RESTful API Engine",
        Version = "v1",
        Description = "Hệ thống RESTful Web API chuẩn hóa phục vụ Web App (ASP.NET Core 10 MVC) và Mobile App (.NET MAUI 10)."
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Description = "Nhập Token ủy quyền theo chuẩn: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", null)] = new List<string>()
    });

    // Only document RESTful API endpoints in Swagger, skipping MVC View controllers
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return (apiDesc.RelativePath != null && apiDesc.RelativePath.StartsWith("api", StringComparison.OrdinalIgnoreCase))
            || apiDesc.ActionDescriptor.EndpointMetadata.OfType<Microsoft.AspNetCore.Mvc.ApiControllerAttribute>().Any();
    });
});

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
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new GeoIpRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// Persist DataProtection keys so container restarts/rebuilds do not invalidate Antiforgery or Auth cookies
var keysFolder = Environment.GetEnvironmentVariable("DATAPROTECTION_KEYS_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, ".keys");
Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .SetApplicationName("NET-Tutos")
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

// Antiforgery Configuration for AJAX & Fetch requests
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "NetTutos.Antiforgery";
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
#pragma warning disable ASPDEPR005
    options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
    options.KnownProxies.Clear();
});

builder.Services.AddSingleton<IMarkdownService, MarkdownService>();
builder.Services.AddScoped<ITutorialService, TutorialService>();
builder.Services.AddScoped<ILearningProgressService, LearningProgressService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddHttpClient("CodeRunner").AddHttpMessageHandler<ResilientMicroserviceHandler>();
builder.Services.AddHttpClient("Gamification").AddHttpMessageHandler<ResilientMicroserviceHandler>();
builder.Services.AddHttpClient("AiTutor").AddHttpMessageHandler<ResilientMicroserviceHandler>();
builder.Services.AddHttpClient("Catalog").AddHttpMessageHandler<ResilientMicroserviceHandler>();
builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ICodeExecutionService, CodeExecutionService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IDiscussionService, DiscussionService>();
builder.Services.AddScoped<IStreakService, StreakService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IActivityFeedService, ActivityFeedService>();
builder.Services.AddScoped<ICapstoneProjectService, CapstoneProjectService>();
builder.Services.AddScoped<IFlashcardService, FlashcardService>();
builder.Services.AddScoped<ICurriculumService, CurriculumService>();
builder.Services.AddScoped<ISemanticCurriculumSearchService, SemanticCurriculumSearchService>();
builder.Services.AddScoped<IStudyPlannerService, StudyPlannerService>();
builder.Services.AddScoped<AiTutorService>();
builder.Services.AddScoped<IAiTutorService, GeminiAgentService>();
builder.Services.AddScoped<IAiConversationService, AiConversationService>();
builder.Services.AddScoped<IDeveloperPortfolioService, DeveloperPortfolioService>();
builder.Services.AddScoped<IMobileAuthService, MobileAuthService>();
builder.Services.AddSingleton<INavMenuService, NavMenuService>();
builder.Services.AddSingleton<IGeoIpService, GeoIpService>();
builder.Services.AddSingleton<ISeoMetadataService, SeoMetadataService>();
builder.Services.AddSingleton<ICodingArenaService>(sp => new CodingArenaService(sp.GetRequiredService<IServiceScopeFactory>()));

// Determine Database Provider
var configuredProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "PostgreSql";
var postgreSqlConnection = builder.Configuration.GetConnectionString("PostgreSqlConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=nettutos_db;Username=postgres;Password=postgres;Include Error Detail=true";
var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServerConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=DotNetTutorialsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection") 
    ?? "Data Source=dotnet_tutorials.db";

string activeProvider = "PostgreSQL";
string activeProviderEn = "PostgreSQL";

if (configuredProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) || configuredProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
{
    bool isPostgresAvailable = false;
    try
    {
        var npgsqlBuilder = new NpgsqlConnectionStringBuilder(postgreSqlConnection)
        {
            Timeout = 3,
            CommandTimeout = 3
        };
        using var testConn = new NpgsqlConnection(npgsqlBuilder.ConnectionString);
        testConn.Open();
        isPostgresAvailable = true;
    }
    catch
    {
        isPostgresAvailable = false;
    }

    if (isPostgresAvailable)
    {
        activeProvider = "PostgreSQL";
        activeProviderEn = "PostgreSQL";
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(postgreSqlConnection));
    }
    else
    {
        activeProvider = "SQLite (Fallback - Khởi động Docker Compose để kích hoạt PostgreSQL)";
        activeProviderEn = "SQLite (Fallback - Start Docker Compose to activate PostgreSQL)";
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(sqliteConnection));
    }
}
else if (configuredProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
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

// Configure OAuth 2.0 External Authentication Providers (GitHub & Google)
static string FirstNonEmpty(params string?[] values) =>
    values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;

var githubClientId = FirstNonEmpty(
    Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID"),
    builder.Configuration["GITHUB_CLIENT_ID"],
    builder.Configuration["Authentication:GitHub:ClientId"]);
var githubClientSecret = FirstNonEmpty(
    Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET"),
    builder.Configuration["GITHUB_CLIENT_SECRET"],
    builder.Configuration["Authentication:GitHub:ClientSecret"]);
var googleClientId = FirstNonEmpty(
    Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"),
    builder.Configuration["GOOGLE_CLIENT_ID"],
    builder.Configuration["Authentication:Google:ClientId"]);
var googleClientSecret = FirstNonEmpty(
    Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET"),
    builder.Configuration["GOOGLE_CLIENT_SECRET"],
    builder.Configuration["Authentication:Google:ClientSecret"]);

static bool IsValidOAuthCredential(string id, string secret) =>
    !string.IsNullOrWhiteSpace(id) &&
    !string.IsNullOrWhiteSpace(secret) &&
    !id.StartsWith("your_", StringComparison.OrdinalIgnoreCase) &&
    !secret.StartsWith("your_", StringComparison.OrdinalIgnoreCase);

bool isGitHubOAuthConfigured = IsValidOAuthCredential(githubClientId, githubClientSecret);
bool isGoogleOAuthConfigured = IsValidOAuthCredential(googleClientId, googleClientSecret);

builder.Services.AddSingleton(new ExternalOAuthConfig
{
    IsGitHubConfigured = isGitHubOAuthConfigured,
    IsGoogleConfigured = isGoogleOAuthConfigured
});

var authBuilder = builder.Services.AddAuthentication();

authBuilder.AddOAuth("GitHub", "GitHub", options =>
{
    options.ClientId = isGitHubOAuthConfigured ? githubClientId : "unconfigured-github-client-id";
    options.ClientSecret = isGitHubOAuthConfigured ? githubClientSecret : "unconfigured-github-client-secret";
    options.CallbackPath = new PathString("/signin-github");
    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
    options.UserInformationEndpoint = "https://api.github.com/user";
    options.SignInScheme = IdentityConstants.ExternalScheme;
    options.SaveTokens = true;
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.Scope.Add("read:user");
    options.Scope.Add("user:email");

    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    options.ClaimActions.MapJsonKey("urn:github:login", "login");
    options.ClaimActions.MapJsonKey("urn:oauth:avatar", "avatar_url");

    options.Events = new OAuthEvents
    {
        OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Account/ExternalLoginCallback?remoteError=github_auth_cancelled");
            context.HandleResponse();
            return Task.CompletedTask;
        },
        OnCreatingTicket = async context =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("NET-Tutos-OAuth/1.0");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

            using var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();

            using var userJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
            context.RunClaimActions(userJson.RootElement);

            // If primary email is private on GitHub, fetch verified primary email from /user/emails
            if (string.IsNullOrEmpty(context.Identity?.FindFirst(ClaimTypes.Email)?.Value))
            {
                using var emailReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                emailReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                emailReq.Headers.UserAgent.ParseAdd("NET-Tutos-OAuth/1.0");
                emailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                using var emailRes = await context.Backchannel.SendAsync(emailReq, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                if (emailRes.IsSuccessStatusCode)
                {
                    using var emailsDoc = JsonDocument.Parse(await emailRes.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                    foreach (var element in emailsDoc.RootElement.EnumerateArray())
                    {
                        var isPrimary = element.TryGetProperty("primary", out var p) && p.GetBoolean();
                        var emailVal = element.TryGetProperty("email", out var e) ? e.GetString() : null;
                        if (isPrimary && !string.IsNullOrWhiteSpace(emailVal))
                        {
                            context.Identity?.AddClaim(new Claim(ClaimTypes.Email, emailVal));
                            break;
                        }
                    }
                }
            }
        }
    };
});

authBuilder.AddOAuth("Google", "Google", options =>
{
    options.ClientId = isGoogleOAuthConfigured ? googleClientId : "unconfigured-google-client-id";
    options.ClientSecret = isGoogleOAuthConfigured ? googleClientSecret : "unconfigured-google-client-secret";
    options.CallbackPath = new PathString("/signin-google");
    options.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    options.TokenEndpoint = "https://oauth2.googleapis.com/token";
    options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
    options.SignInScheme = IdentityConstants.ExternalScheme;
    options.SaveTokens = true;
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    options.ClaimActions.MapJsonKey("urn:oauth:avatar", "picture");

    options.Events = new OAuthEvents
    {
        OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Account/ExternalLoginCallback?remoteError=google_auth_cancelled");
            context.HandleResponse();
            return Task.CompletedTask;
        },
        OnCreatingTicket = async context =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

            using var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();

            using var userJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
            context.RunClaimActions(userJson.RootElement);
        }
    };
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

app.UseForwardedHeaders();

var configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? string.Empty;
if (configuredUrls.Contains("https://", StringComparison.OrdinalIgnoreCase) ||
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT")))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

var locOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);
app.Use(async (context, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var country = context.GetDetectedCountry();
    var isEn = context.IsEnglish();
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Detected-Country"] = country;
        context.Response.Headers["Content-Language"] = isEn ? "en-US" : "vi-VN";
        return Task.CompletedTask;
    });
    await next();
    sw.Stop();
    var obs = context.RequestServices.GetService<IObservabilityService>();
    obs?.RecordHttpRequest(context.Request.Path, context.Response.StatusCode, sw.Elapsed.TotalMilliseconds);
});

app.UseRouting();

// Enable CORS for Next.js frontend and mobile
app.UseCors("FrontendPolicy");

// Enable Swagger API Documentation & Explorer UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET Tutos API Engine v1");
    c.RoutePrefix = "swagger";
});

// Authentication must be before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Web API Controllers
app.MapControllers();

// OpenTelemetry & HTTP Resilience Diagnostics Endpoint
app.MapGet("/api/telemetry/diagnostics", async (IObservabilityService observability, CancellationToken ct) =>
{
    var snapshot = await observability.GetDiagnosticsAsync(ct);
    return Results.Ok(snapshot);
});

// RAG Semantic Curriculum Search Endpoint (hashed TF-IDF vector index + cosine similarity)
app.MapGet("/api/search/semantic", async (string? q, int? top, HttpContext http, ISemanticCurriculumSearchService search, CancellationToken ct) =>
{
    var result = await search.SearchAsync(q ?? string.Empty, http.IsEnglish(), top ?? 5, ct);
    return Results.Ok(result);
});

// Seed/Sync additional tutorials endpoint
app.MapGet("/api/admin/seed-tutorials", async (AppDbContext db) =>
{
    await AdditionalTutorialSeeder.SeedAdditionalTutorialsAsync(db);
    var count = await db.Tutorials.CountAsync();
    return Results.Ok(new { message = "Seeded successfully", totalTutorials = count });
});

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
    name: "streak-vi",
    pattern: "chuoi-ngay-hoc",
    defaults: new { controller = "Streak", action = "Index" });

app.MapControllerRoute(
    name: "streak-en",
    pattern: "streak",
    defaults: new { controller = "Streak", action = "Index" });

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

app.MapControllerRoute(
    name: "arena-vi",
    pattern: "dau-truong-code",
    defaults: new { controller = "Arena", action = "Index" });

app.MapControllerRoute(
    name: "arena-en",
    pattern: "coding-arena",
    defaults: new { controller = "Arena", action = "Index" });

// Friendly certificate verify route: /verify-certificate/{code}
app.MapControllerRoute(
    name: "certificate-verify",
    pattern: "verify-certificate/{code}",
    defaults: new { controller = "Certificate", action = "Verify" });

// SignalR Hub endpoints
app.MapHub<DiscussionHub>("/hubs/discussion");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<CodingArenaHub>("/hubs/arena");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
