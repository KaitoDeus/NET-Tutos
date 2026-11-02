using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Application.Services;

namespace NET_Tutos.Gamification.Application.Features.Leaderboard.Queries;

public record GetLeaderboardQuery(int Top = 50);

public class GetLeaderboardQueryHandler
{
    private readonly IGamificationService _gamificationService;

    public GetLeaderboardQueryHandler(IGamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    public async Task<List<LeaderboardEntryDto>> HandleAsync(GetLeaderboardQuery query)
    {
        return await _gamificationService.GetLeaderboardAsync(query.Top);
    }
}
