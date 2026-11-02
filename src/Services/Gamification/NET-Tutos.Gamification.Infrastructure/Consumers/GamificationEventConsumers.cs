using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NET_Tutos.BuildingBlocks.EventBus.Events;
using NET_Tutos.Gamification.Application.Services;
using NET_Tutos.Gamification.Domain.Entities;
using NET_Tutos.Gamification.Infrastructure.Data;

namespace NET_Tutos.Gamification.Infrastructure.Consumers;

public class ChallengeSolvedConsumer : IConsumer<ChallengeSolvedIntegrationEvent>
{
    private readonly IGamificationService _gamificationService;
    private readonly GamificationDbContext _context;
    private readonly ILogger<ChallengeSolvedConsumer> _logger;

    public ChallengeSolvedConsumer(
        IGamificationService gamificationService,
        GamificationDbContext context,
        ILogger<ChallengeSolvedConsumer> logger)
    {
        _gamificationService = gamificationService;
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ChallengeSolvedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Received ChallengeSolvedIntegrationEvent: User {UserId}, Challenge {Title}", msg.UserId, msg.ChallengeTitle);

        await _gamificationService.AwardXpAsync(msg.UserId, msg.XpAwarded, $"Solved Challenge: {msg.ChallengeTitle}");

        // Check if user unlocked code-master badge
        var badge = await _context.Badges.FirstOrDefaultAsync(b => b.Code == "code-master");
        if (badge != null)
        {
            bool hasBadge = await _context.UserBadges.AnyAsync(ub => ub.UserId == msg.UserId && ub.BadgeId == badge.Id);
            if (!hasBadge)
            {
                _context.UserBadges.Add(new UserBadge
                {
                    UserId = msg.UserId,
                    BadgeId = badge.Id,
                    UnlockedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} unlocked code-master badge!", msg.UserId);
            }
        }
    }
}

public class LessonCompletedConsumer : IConsumer<LessonCompletedIntegrationEvent>
{
    private readonly IGamificationService _gamificationService;
    private readonly ILogger<LessonCompletedConsumer> _logger;

    public LessonCompletedConsumer(
        IGamificationService gamificationService,
        ILogger<LessonCompletedConsumer> logger)
    {
        _gamificationService = gamificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LessonCompletedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Received LessonCompletedIntegrationEvent: User {UserId}, Lesson {Title}", msg.UserId, msg.TutorialTitle);

        await _gamificationService.AwardXpAsync(msg.UserId, msg.XpAwarded, $"Completed Lesson: {msg.TutorialTitle}");
    }
}

public class ExamPassedConsumer : IConsumer<ExamPassedIntegrationEvent>
{
    private readonly IGamificationService _gamificationService;
    private readonly ILogger<ExamPassedConsumer> _logger;

    public ExamPassedConsumer(
        IGamificationService gamificationService,
        ILogger<ExamPassedConsumer> logger)
    {
        _gamificationService = gamificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ExamPassedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Received ExamPassedIntegrationEvent: User {UserId}, Exam {Title}, Score {Score}%", msg.UserId, msg.ExamTitle, msg.ScorePercentage);

        await _gamificationService.AwardXpAsync(msg.UserId, msg.XpAwarded, $"Passed Exam: {msg.ExamTitle} ({msg.ScorePercentage}%)");
    }
}
