using Microsoft.AspNetCore.Mvc;
using NET_Tutos.BuildingBlocks.Common.Models;
using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Application.Services;

namespace NET_Tutos.Gamification.API.Endpoints;

public static class LeaderboardEndpoints
{
    public static IEndpointRouteBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gamification/leaderboard")
                             .WithTags("Leaderboard");

        group.MapGet("/", async ([FromQuery] int top = 50, [FromServices] IGamificationService service = null!) =>
        {
            var limit = top > 0 ? top : 50;
            var leaderboard = await service.GetLeaderboardAsync(limit);
            return Results.Ok(ApiResponse<List<LeaderboardEntryDto>>.Ok(leaderboard));
        })
        .WithName("GetLeaderboard")
        .WithSummary("Retrieve global learner XP leaderboard.");

        return endpoints;
    }
}
