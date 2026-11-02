using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Application.Services;

namespace NET_Tutos.Gamification.Application.Features.Badges.Queries;

public record GetUserBadgesQuery(string? UserId);

public class GetUserBadgesQueryHandler
{
    private readonly IGamificationService _gamificationService;

    public GetUserBadgesQueryHandler(IGamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    public async Task<List<BadgeDto>> HandleAsync(GetUserBadgesQuery query)
    {
        return await _gamificationService.GetBadgesAsync(query.UserId);
    }
}
