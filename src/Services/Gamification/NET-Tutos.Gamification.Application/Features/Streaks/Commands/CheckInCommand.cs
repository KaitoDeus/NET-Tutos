using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Application.Services;

namespace NET_Tutos.Gamification.Application.Features.Streaks.Commands;

public record CheckInCommand(string UserId);

public class CheckInCommandHandler
{
    private readonly IGamificationService _gamificationService;

    public CheckInCommandHandler(IGamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    public async Task<CheckInResultDto> HandleAsync(CheckInCommand command)
    {
        return await _gamificationService.CheckInTodayAsync(command.UserId);
    }
}
