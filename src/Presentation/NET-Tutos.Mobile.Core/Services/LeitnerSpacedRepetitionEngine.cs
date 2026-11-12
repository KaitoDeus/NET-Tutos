using NET_Tutos.Mobile.Core.Models;

namespace NET_Tutos.Mobile.Core.Services;

public sealed record LeitnerReviewOutcome(
    int NewBoxLevel,
    int IntervalDays,
    int XpEarned,
    bool IsMastered,
    DateTime NextReviewUtc);

/// <summary>
/// Deterministic 5-box Leitner spaced-repetition scheduler synchronized with Web FlashcardService intervals.
/// </summary>
public static class LeitnerSpacedRepetitionEngine
{
    public static LeitnerReviewOutcome CalculateNextReview(int currentBoxLevel, int rating, DateTime utcNow)
    {
        int clampedCurrent = Math.Clamp(currentBoxLevel, 1, 5);
        int clampedRating = Math.Clamp(rating, 1, 4);

        int newBox = clampedCurrent;
        int nextDays = 1;
        int xpEarned = 5;
        bool isMastered = false;

        switch (clampedRating)
        {
            case 1: // Again
                newBox = 1;
                nextDays = 1;
                xpEarned = 2;
                isMastered = false;
                break;

            case 2: // Hard
                newBox = Math.Max(1, clampedCurrent);
                nextDays = 3;
                xpEarned = 3;
                isMastered = false;
                break;

            case 3: // Good
                newBox = Math.Min(5, clampedCurrent + 1);
                nextDays = newBox switch
                {
                    1 => 2,
                    2 => 5,
                    3 => 9,
                    4 => 16,
                    _ => 30
                };
                xpEarned = 5;
                isMastered = newBox >= 4;
                break;

            case 4: // Mastered
                newBox = Math.Min(5, Math.Max(4, clampedCurrent + 2));
                nextDays = 21;
                xpEarned = 10;
                isMastered = true;
                break;
        }

        return new LeitnerReviewOutcome(
            NewBoxLevel: newBox,
            IntervalDays: nextDays,
            XpEarned: xpEarned,
            IsMastered: isMastered,
            NextReviewUtc: utcNow.AddDays(nextDays));
    }

    public static LeitnerReviewOutcome ApplyLocalReview(OfflineFlashcardRecord card, int rating, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(card);
        var outcome = CalculateNextReview(card.BoxLevel, rating, utcNow);
        card.BoxLevel = outcome.NewBoxLevel;
        card.ReviewCount += 1;
        card.IsMastered = card.IsMastered || outcome.IsMastered;
        card.NextReviewDate = outcome.NextReviewUtc;
        card.IsDue = false;
        return outcome;
    }

    public static List<OfflineFlashcardRecord> OrderForStudySession(
        IEnumerable<OfflineFlashcardRecord> cards,
        DateTime utcNow,
        bool dueOnly = false)
    {
        var list = cards.Select(c =>
        {
            c.IsDue = !c.NextReviewDate.HasValue || c.NextReviewDate.Value <= utcNow;
            return c;
        });

        if (dueOnly)
        {
            list = list.Where(c => c.IsDue);
        }

        return list
            .OrderByDescending(c => c.IsDue)
            .ThenBy(c => c.NextReviewDate ?? DateTime.MinValue)
            .ThenBy(c => c.OrderIndex)
            .ToList();
    }
}
