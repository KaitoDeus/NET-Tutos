namespace NET_Tutos.Gamification.Domain.Exceptions;

public class StreakAlreadyClaimedException : GamificationDomainException
{
    public string UserId { get; }
    public DateTime ClaimedDate { get; }

    public StreakAlreadyClaimedException(string userId, DateTime claimedDate)
        : base($"Daily streak has already been claimed today for user '{userId}' on {claimedDate:yyyy-MM-dd}.")
    {
        UserId = userId;
        ClaimedDate = claimedDate;
    }
}
