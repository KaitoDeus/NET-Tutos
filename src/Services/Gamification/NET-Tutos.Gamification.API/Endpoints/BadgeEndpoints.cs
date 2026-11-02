using Microsoft.AspNetCore.Mvc;
using NET_Tutos.BuildingBlocks.Common.Models;
using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Application.Services;

namespace NET_Tutos.Gamification.API.Endpoints;

public static class BadgeEndpoints
{
    public static IEndpointRouteBuilder MapBadgeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gamification/badges")
                             .WithTags("Badges");

        group.MapGet("/", async ([FromQuery] string? userId, [FromServices] IGamificationService service) =>
        {
            var badges = await service.GetBadgesAsync(userId);
            return Results.Ok(ApiResponse<List<BadgeDto>>.Ok(badges));
        })
        .WithName("GetBadges")
        .WithSummary("Retrieve catalog of achievement badges and unlocked states.");

        return endpoints;
    }
}
