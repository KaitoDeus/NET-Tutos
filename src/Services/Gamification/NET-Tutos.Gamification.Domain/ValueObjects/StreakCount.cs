using NET_Tutos.Gamification.Domain.Common;

namespace NET_Tutos.Gamification.Domain.ValueObjects;

public class StreakCount : ValueObject
{
    public int Days { get; }

    public StreakCount(int days)
    {
        Days = Math.Max(0, days);
    }

    public static StreakCount Zero => new(0);

    public StreakCount Increment() => new(Days + 1);

    public StreakCount Reset() => new(1);

    public bool IsMilestone() => Days is 3 or 7 or 14 or 30 or 60 or 100 or 365;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Days;
    }

    public override string ToString() => $"{Days} Days";

    public static implicit operator int(StreakCount streak) => streak.Days;
    public static explicit operator StreakCount(int days) => new(days);
}
