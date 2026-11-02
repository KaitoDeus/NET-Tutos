using NET_Tutos.Gamification.Application.Contracts;
using NET_Tutos.Gamification.Application.Services;

namespace NET_Tutos.Gamification.Application.Features.Xp.Commands;

public record AwardXpCommand(string UserId, int XpAmount, string Reason);

public class AwardXpCommandHandler
{
    private readonly IGamificationService _gamificationService;

    public AwardXpCommandHandler(IGamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    public async Task<UserProgressDto> HandleAsync(AwardXpCommand command)
    {
        return await _gamificationService.AwardXpAsync(command.UserId, command.XpAmount, command.Reason);
    }
}
