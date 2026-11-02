using Microsoft.AspNetCore.Mvc;
using NET_Tutos.BuildingBlocks.Common.Models;
using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Application.Services;

namespace NET_Tutos.Gamification.API.Endpoints;

public static class StreakEndpoints
{
    public static IEndpointRouteBuilder MapStreakEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gamification/streak")
                             .WithTags("Streak");

        group.MapGet("/{userId}", async (string userId, [FromServices] IGamificationService service) =>
        {
            var streak = await service.GetUserStreakAsync(userId);
            return Results.Ok(ApiResponse<StreakDto>.Ok(streak));
        })
        .WithName("GetUserStreak")
        .WithSummary("Retrieve current and longest streak for a learner.");

        group.MapPost("/check-in", async ([FromBody] string userId, [FromServices] IGamificationService service) =>
        {
            var result = await service.CheckInTodayAsync(userId);
            return Results.Ok(ApiResponse<CheckInResultDto>.Ok(result));
        })
        .WithName("CheckInDailyStreak")
        .WithSummary("Record daily check-in and claim streak bonus XP.");

        return endpoints;
    }
}
