using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NET_Tutos.Gamification.API.Endpoints;
using NET_Tutos.Gamification.API.Extensions;
using NET_Tutos.Gamification.API.Middlewares;
using NET_Tutos.Gamification.Domain.Entities;
using NET_Tutos.Gamification.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure & Application Services
builder.Services.AddGamificationInfrastructure(builder.Configuration);

var app = builder.Build();

// Global Exception Handler Middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowAll");

// Auto-migrate / Ensure Database schema & tables
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GamificationDbContext>();
    try
    {
        if (db.Database.IsRelational())
        {
            var creator = db.Database.GetService<IRelationalDatabaseCreator>();
            try
            {
                creator.CreateTables();
            }
            catch
            {
                // Tables may already exist
            }
        }
        else
        {
            db.Database.EnsureCreated();
        }

        // Seed default badges if not present
        if (!db.Badges.Any())
        {
            db.Badges.AddRange(
                new Badge { Code = "streak-3", Name = "Khởi đầu kiên trì", Description = "Duy trì chuỗi học 3 ngày liên tiếp", IconClass = "bi-fire", BadgeColor = "warning", RequiredCount = 3 },
                new Badge { Code = "streak-7", Name = "Chiến binh 1 tuần", Description = "Duy trì chuỗi học 7 ngày liên tiếp", IconClass = "bi-lightning-charge-fill", BadgeColor = "danger", RequiredCount = 7 },
                new Badge { Code = "streak-14", Name = "Bất khả chiến bại", Description = "Duy trì chuỗi học 14 ngày liên tiếp", IconClass = "bi-trophy-fill", BadgeColor = "primary", RequiredCount = 14 },
                new Badge { Code = "streak-30", Name = "Huyền thoại .NET", Description = "Duy trì chuỗi học 30 ngày liên tiếp", IconClass = "bi-star-fill", BadgeColor = "success", RequiredCount = 30 },
                new Badge { Code = "code-master", Name = "Bậc thầy Thuật toán", Description = "Giải thành công 10 thử thách lập trình", IconClass = "bi-terminal-fill", BadgeColor = "info", RequiredCount = 10 },
                new Badge { Code = "scholar", Name = "Học giả .NET", Description = "Hoàn thành 20 bài học giáo trình", IconClass = "bi-book-fill", BadgeColor = "secondary", RequiredCount = 20 }
            );
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database schema check or seed warning.");
    }
}

// Health check
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", service = "Gamification.API", timestamp = DateTime.UtcNow }));

// Map Endpoint Modules
app.MapStreakEndpoints();
app.MapBadgeEndpoints();
app.MapLeaderboardEndpoints();

app.Run();

// Marker for integration tests
public partial class Program { }
