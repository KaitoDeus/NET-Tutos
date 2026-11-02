using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.BuildingBlocks.Common.Models;
using NET_Tutos.Gamification.Application.Common.Interfaces;
using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Application.Services;
using NET_Tutos.Gamification.Infrastructure.Consumers;
using NET_Tutos.Gamification.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure Database
var postgresConn = builder.Configuration.GetConnectionString("PostgreSqlConnection");
builder.Services.AddDbContext<GamificationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(postgresConn))
    {
        options.UseNpgsql(postgresConn);
    }
    else
    {
        options.UseInMemoryDatabase("GamificationDb");
    }
});

builder.Services.AddScoped<IGamificationDbContext>(sp => sp.GetRequiredService<GamificationDbContext>());

// Configure MassTransit with Consumers
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ChallengeSolvedConsumer>();
    x.AddConsumer<LessonCompletedConsumer>();
    x.AddConsumer<ExamPassedConsumer>();

    if (builder.Configuration.GetValue<bool>("UseInMemoryBus", false) || string.IsNullOrEmpty(builder.Configuration["RabbitMQ:Host"]))
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(rabbitMqHost, "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
            cfg.ConfigureEndpoints(context);
        });
    }
});

// Register Domain Services
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Ensure Database & Seed Data created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GamificationDbContext>();
    db.Database.EnsureCreated();
}

// Health check
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", service = "Gamification.API", timestamp = DateTime.UtcNow }));

// Streak Endpoints
app.MapGet("/api/v1/gamification/streak/{userId}", async (string userId, [FromServices] IGamificationService service) =>
{
    var streak = await service.GetUserStreakAsync(userId);
    return Results.Ok(ApiResponse<StreakDto>.Ok(streak));
});

app.MapPost("/api/v1/gamification/streak/check-in", async ([FromBody] string userId, [FromServices] IGamificationService service) =>
{
    var result = await service.CheckInTodayAsync(userId);
    return Results.Ok(ApiResponse<CheckInResultDto>.Ok(result));
});

// Badges & Leaderboard Endpoints
app.MapGet("/api/v1/gamification/badges", async ([FromQuery] string? userId, [FromServices] IGamificationService service) =>
{
    var badges = await service.GetBadgesAsync(userId);
    return Results.Ok(ApiResponse<List<BadgeDto>>.Ok(badges));
});

app.MapGet("/api/v1/gamification/leaderboard", async ([FromQuery] int top, [FromServices] IGamificationService service) =>
{
    int limit = top > 0 ? Math.Min(top, 100) : 50;
    var leaderboard = await service.GetLeaderboardAsync(limit);
    return Results.Ok(ApiResponse<List<LeaderboardEntryDto>>.Ok(leaderboard));
});

app.Run();

public partial class Program { }
