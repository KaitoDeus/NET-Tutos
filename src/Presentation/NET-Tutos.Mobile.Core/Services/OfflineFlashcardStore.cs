using System.Text.Json;
using NET_Tutos.Mobile.Core.Models;

namespace NET_Tutos.Mobile.Core.Services;

/// <summary>
/// Thread-safe local JSON persistence for offline flashcards and pending spaced-repetition review outbox synchronization.
/// </summary>
public sealed class OfflineFlashcardStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _storageDirectory;
    private readonly string _deckFilePath;
    private readonly string _outboxFilePath;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    public OfflineFlashcardStore(string storageDirectory)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException("Storage directory path cannot be empty.", nameof(storageDirectory));
        }

        _storageDirectory = storageDirectory;
        Directory.CreateDirectory(_storageDirectory);
        _deckFilePath = Path.Combine(_storageDirectory, "flashcards_deck.json");
        _outboxFilePath = Path.Combine(_storageDirectory, "flashcards_outbox.json");
    }

    public async Task SaveDeckAsync(IEnumerable<OfflineFlashcardRecord> cards, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var incoming = cards.ToList();
            var existing = await ReadJsonListUnsafeAsync<OfflineFlashcardRecord>(_deckFilePath, cancellationToken).ConfigureAwait(false);
            var outbox = await ReadJsonListUnsafeAsync<QueuedFlashcardReview>(_outboxFilePath, cancellationToken).ConfigureAwait(false);
            var dirtyCardIds = outbox.Select(r => r.FlashcardId).ToHashSet();

            // Merge strategy: preserve newer local spaced-repetition state when queued reviews have not synced yet.
            var existingMap = existing.ToDictionary(c => c.Id);
            var merged = incoming.Select(card =>
            {
                if (dirtyCardIds.Contains(card.Id) && existingMap.TryGetValue(card.Id, out var local))
                {
                    return local;
                }

                return card;
            }).ToList();

            foreach (var oldCard in existing.Where(e => merged.All(m => m.Id != e.Id)))
            {
                merged.Add(oldCard);
            }

            await WriteJsonListUnsafeAsync(_deckFilePath, merged, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<List<OfflineFlashcardRecord>> LoadDeckAsync(
        string? topic = null,
        bool dueOnly = false,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var cards = await ReadJsonListUnsafeAsync<OfflineFlashcardRecord>(_deckFilePath, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(topic))
            {
                cards = cards
                    .Where(c => string.Equals(c.Topic, topic, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return LeitnerSpacedRepetitionEngine.OrderForStudySession(cards, utcNow ?? DateTime.UtcNow, dueOnly);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<LeitnerReviewOutcome> EnqueueOfflineReviewAsync(
        int flashcardId,
        int rating,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var timestamp = utcNow ?? DateTime.UtcNow;

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var deck = await ReadJsonListUnsafeAsync<OfflineFlashcardRecord>(_deckFilePath, cancellationToken).ConfigureAwait(false);
            var targetCard = deck.FirstOrDefault(c => c.Id == flashcardId);

            int currentBox = targetCard?.BoxLevel ?? 1;
            var outcome = targetCard != null
                ? LeitnerSpacedRepetitionEngine.ApplyLocalReview(targetCard, rating, timestamp)
                : LeitnerSpacedRepetitionEngine.CalculateNextReview(currentBox, rating, timestamp);

            if (targetCard != null)
            {
                await WriteJsonListUnsafeAsync(_deckFilePath, deck, cancellationToken).ConfigureAwait(false);
            }

            var outbox = await ReadJsonListUnsafeAsync<QueuedFlashcardReview>(_outboxFilePath, cancellationToken).ConfigureAwait(false);
            outbox.Add(new QueuedFlashcardReview
            {
                ClientReviewId = Guid.NewGuid().ToString("N"),
                FlashcardId = flashcardId,
                Rating = Math.Clamp(rating, 1, 4),
                ReviewedAtUtc = timestamp
            });

            await WriteJsonListUnsafeAsync(_outboxFilePath, outbox, cancellationToken).ConfigureAwait(false);
            return outcome;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<int> GetPendingReviewCountAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var outbox = await ReadJsonListUnsafeAsync<QueuedFlashcardReview>(_outboxFilePath, cancellationToken).ConfigureAwait(false);
            return outbox.Count;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<List<QueuedFlashcardReview>> GetPendingReviewsAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadJsonListUnsafeAsync<QueuedFlashcardReview>(_outboxFilePath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<OfflineSyncFlushResult> FlushPendingReviewsAsync(
        Func<BatchFlashcardReviewRequest, CancellationToken, Task<BatchFlashcardReviewResponse?>> syncTransport,
        string language = "vi",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(syncTransport);

        List<QueuedFlashcardReview> snapshot;
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            snapshot = await ReadJsonListUnsafeAsync<QueuedFlashcardReview>(_outboxFilePath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }

        if (snapshot.Count == 0)
        {
            return new OfflineSyncFlushResult
            {
                Attempted = false,
                IsSuccess = true,
                Message = language == "en" ? "All flashcards are up to date." : "Mọi thẻ đã được đồng bộ."
            };
        }

        var response = await syncTransport(
            new BatchFlashcardReviewRequest
            {
                Reviews = snapshot,
                Language = language
            },
            cancellationToken).ConfigureAwait(false);

        if (response == null || !response.IsSuccess)
        {
            return new OfflineSyncFlushResult
            {
                Attempted = true,
                IsSuccess = false,
                RemainingCount = snapshot.Count,
                Message = response?.Message ?? (language == "en" ? "Unable to reach server for sync." : "Chưa thể kết nối máy chủ để đồng bộ.")
            };
        }

        var syncedIds = response.Results
            .Where(r => r.IsSuccess && !string.IsNullOrWhiteSpace(r.ClientReviewId))
            .Select(r => r.ClientReviewId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var currentOutbox = await ReadJsonListUnsafeAsync<QueuedFlashcardReview>(_outboxFilePath, cancellationToken).ConfigureAwait(false);
            var remaining = syncedIds.Count > 0
                ? currentOutbox.Where(q => !syncedIds.Contains(q.ClientReviewId)).ToList()
                : new List<QueuedFlashcardReview>();

            await WriteJsonListUnsafeAsync(_outboxFilePath, remaining, cancellationToken).ConfigureAwait(false);

            // Reconcile server-confirmed BoxLevel & NextReviewDate back onto the cached deck.
            if (response.Results.Count > 0)
            {
                var deck = await ReadJsonListUnsafeAsync<OfflineFlashcardRecord>(_deckFilePath, cancellationToken).ConfigureAwait(false);
                var deckMap = deck.ToDictionary(c => c.Id);
                foreach (var res in response.Results.Where(r => r.IsSuccess))
                {
                    if (deckMap.TryGetValue(res.FlashcardId, out var card))
                    {
                        card.BoxLevel = res.NewBoxLevel;
                        card.IsMastered = res.IsMastered;
                        card.NextReviewDate = res.NextReviewDate;
                        card.IsDue = !res.NextReviewDate.HasValue || res.NextReviewDate.Value <= DateTime.UtcNow;
                    }
                }
                await WriteJsonListUnsafeAsync(_deckFilePath, deck, cancellationToken).ConfigureAwait(false);
            }

            return new OfflineSyncFlushResult
            {
                Attempted = true,
                IsSuccess = true,
                SyncedCount = response.ProcessedCount,
                RemainingCount = remaining.Count,
                XpEarned = response.TotalXpEarned,
                TotalXp = response.TotalXp,
                StreakDays = response.StreakDays,
                Message = response.Message
            };
        }
        finally
        {
            _mutex.Release();
        }
    }

    private static async Task<List<T>> ReadJsonListUnsafeAsync<T>(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            return new List<T>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static async Task WriteJsonListUnsafeAsync<T>(string path, List<T> items, CancellationToken ct)
    {
        var tempPath = path + ".tmp";
        var json = JsonSerializer.Serialize(items, JsonOptions);
        await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
        File.Move(tempPath, path, overwrite: true);
    }
}
