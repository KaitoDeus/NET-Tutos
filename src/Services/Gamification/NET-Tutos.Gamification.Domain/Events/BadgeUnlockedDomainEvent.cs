namespace NET_Tutos.Gamification.Domain.Events;

public record BadgeUnlockedDomainEvent(
    string UserId,
    string BadgeCode,
    string BadgeName,
    DateTime OccurredOn
);
