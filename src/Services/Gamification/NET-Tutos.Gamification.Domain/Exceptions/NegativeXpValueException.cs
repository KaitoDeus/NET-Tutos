namespace NET_Tutos.Gamification.Domain.Exceptions;

public class NegativeXpValueException : GamificationDomainException
{
    public int AttemptedXp { get; }

    public NegativeXpValueException(int attemptedXp)
        : base($"Experience Points (XP) cannot be negative. Attempted value: {attemptedXp}")
    {
        AttemptedXp = attemptedXp;
    }
}
