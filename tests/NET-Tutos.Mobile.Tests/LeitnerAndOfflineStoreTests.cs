using FluentAssertions;
using NET_Tutos.Mobile.Core.Models;
using NET_Tutos.Mobile.Core.Services;
using Xunit;

namespace NET_Tutos.Mobile.Tests;

public sealed class LeitnerAndOfflineStoreTests : IDisposable
{
    private readonly string _tempDir;

    public LeitnerAndOfflineStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"nettutos_mobile_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Theory]
    [InlineData(3, 1, 1, 1, 2, false)]   // Again resets to Box 1, 1 day, +2 XP
    [InlineData(3, 2, 3, 3, 3, false)]   // Hard keeps box, 3 days, +3 XP
    [InlineData(1, 3, 2, 5, 5, false)]   // Good promotes 1 -> 2, 5 days, +5 XP
    [InlineData(3, 3, 4, 16, 5, true)]   // Good promotes 3 -> 4, mastered
    [InlineData(2, 4, 4, 21, 10, true)]  // Mastered jumps to >=4, 21 days, +10 XP
    public void CalculateNextReview_FollowsLeitnerBoxes(
        int currentBox, int rating, int expectedBox, int expectedDays, int expectedXp, bool expectedMastered)
    {
        var now = new DateTime(2026, 11, 12, 8, 0, 0, DateTimeKind.Utc);

        var outcome = LeitnerSpacedRepetitionEngine.CalculateNextReview(currentBox, rating, now);

        outcome.NewBoxLevel.Should().Be(expectedBox);
        outcome.IntervalDays.Should().Be(expectedDays);
        outcome.XpEarned.Should().Be(expectedXp);
        outcome.IsMastered.Should().Be(expectedMastered);
        outcome.NextReviewUtc.Should().Be(now.AddDays(expectedDays));
    }

    [Fact]
    public void OrderForStudySession_PrioritizesDueCards_AndFiltersWhenDueOnly()
    {
        var now = new DateTime(2026, 11, 12, 8, 0, 0, DateTimeKind.Utc);
        var cards = new List<OfflineFlashcardRecord>
        {
            new() { Id = 1, OrderIndex = 1, NextReviewDate = now.AddDays(5) },
            new() { Id = 2, OrderIndex = 2, NextReviewDate = now.AddHours(-2) },
            new() { Id = 3, OrderIndex = 3, NextReviewDate = null }
        };

        var orderedAll = LeitnerSpacedRepetitionEngine.OrderForStudySession(cards, now, dueOnly: false);
        orderedAll.Select(c => c.Id).Should().Equal(3, 2, 1);

        var dueOnly = LeitnerSpacedRepetitionEngine.OrderForStudySession(cards, now, dueOnly: true);
        dueOnly.Select(c => c.Id).Should().Equal(3, 2);
    }

    [Fact]
    public async Task OfflineFlashcardStore_QueuesReviewsPreservesLocalProgressAndFlushesAtomically()
    {
        var store = new OfflineFlashcardStore(_tempDir);
        var now = new DateTime(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);

        await store.SaveDeckAsync(
        [
            new OfflineFlashcardRecord { Id = 10, Topic = "AsyncConcurrency", QuestionVi = "async/await?", BoxLevel = 1, OrderIndex = 1 },
            new OfflineFlashcardRecord { Id = 11, Topic = "EfCoreDatabase", QuestionVi = "AsNoTracking?", BoxLevel = 2, OrderIndex = 2 }
        ]);

        var outcome = await store.EnqueueOfflineReviewAsync(10, rating: 4, utcNow: now);
        outcome.IsMastered.Should().BeTrue();
        outcome.NewBoxLevel.Should().Be(4);
        (await store.GetPendingReviewCountAsync()).Should().Be(1);

        // Saving stale server snapshot must keep dirty local BoxLevel while outbox item is unsynced.
        await store.SaveDeckAsync(
        [
            new OfflineFlashcardRecord { Id = 10, Topic = "AsyncConcurrency", QuestionVi = "async/await?", BoxLevel = 1, IsMastered = false, OrderIndex = 1 }
        ]);
        var deckAfterStaleSave = await store.LoadDeckAsync(topic: "AsyncConcurrency", dueOnly: false, utcNow: now);
        deckAfterStaleSave.Single().BoxLevel.Should().Be(4);
        deckAfterStaleSave.Single().IsMastered.Should().BeTrue();

        // Failed transport must keep item in outbox for future retry.
        var failedFlush = await store.FlushPendingReviewsAsync((_, _) => Task.FromResult<BatchFlashcardReviewResponse?>(null));
        failedFlush.IsSuccess.Should().BeFalse();
        failedFlush.RemainingCount.Should().Be(1);
        (await store.GetPendingReviewCountAsync()).Should().Be(1);

        // Successful transport clears synced item and reconciles confirmed progress.
        var okFlush = await store.FlushPendingReviewsAsync((req, _) => Task.FromResult<BatchFlashcardReviewResponse?>(
            new BatchFlashcardReviewResponse
            {
                IsSuccess = true,
                ProcessedCount = req.Reviews.Count,
                TotalXpEarned = 10,
                TotalXp = 110,
                StreakDays = 4,
                Message = "Synced",
                Results = req.Reviews.Select(r => new BatchFlashcardReviewItemResult
                {
                    ClientReviewId = r.ClientReviewId,
                    FlashcardId = r.FlashcardId,
                    IsSuccess = true,
                    NewBoxLevel = 5,
                    XpEarned = 10,
                    IsMastered = true,
                    NextReviewDate = now.AddDays(21)
                }).ToList()
            }));

        okFlush.IsSuccess.Should().BeTrue();
        okFlush.SyncedCount.Should().Be(1);
        okFlush.RemainingCount.Should().Be(0);
        okFlush.TotalXp.Should().Be(110);
        (await store.GetPendingReviewCountAsync()).Should().Be(0);

        var syncedCard = (await store.LoadDeckAsync(topic: "AsyncConcurrency", utcNow: now)).Single();
        syncedCard.BoxLevel.Should().Be(5);
    }
}
