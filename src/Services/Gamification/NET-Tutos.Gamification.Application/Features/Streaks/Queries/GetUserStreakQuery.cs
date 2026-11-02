using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Application.Services;

namespace NET_Tutos.Gamification.Application.Features.Streaks.Queries;

public record GetUserStreakQuery(string UserId);

public class GetUserStreakQueryHandler
{
    private readonly IGamificationService _gamificationService;

    public GetUserStreakQueryHandler(IGamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    public async Task<StreakDto> HandleAsync(GetUserStreakQuery query)
    {
        return await _gamificationService.GetUserStreakAsync(query.UserId);
    }
}
