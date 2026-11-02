using NET_Tutos.Gamification.API.Endpoints;
using NET_Tutos.Gamification.API.Extensions;
using NET_Tutos.Gamification.API.Middlewares;
using NET_Tutos.Gamification.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure & Application Services
builder.Services.AddGamificationInfrastructure(builder.Configuration);

var app = builder.Build();

// Global Exception Handler Middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowAll");

// Auto-migrate / Ensure Database schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GamificationDbContext>();
    db.Database.EnsureCreated();
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
