namespace NET_Tutos.Gamification.Domain.Events;

public record StreakMilestoneReachedDomainEvent(
    string UserId,
    int StreakDays,
    DateTime OccurredOn
);
