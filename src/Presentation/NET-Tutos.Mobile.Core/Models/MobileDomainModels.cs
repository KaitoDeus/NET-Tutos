namespace NET_Tutos.Mobile.Core.Models;

public sealed class OfflineFlashcardRecord
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string QuestionVi { get; set; } = string.Empty;
    public string QuestionEn { get; set; } = string.Empty;
    public string AnswerMarkdownVi { get; set; } = string.Empty;
    public string AnswerMarkdownEn { get; set; } = string.Empty;
    public string InterviewTipVi { get; set; } = string.Empty;
    public string InterviewTipEn { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int BoxLevel { get; set; } = 1;
    public bool IsMastered { get; set; }
    public int ReviewCount { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public bool IsDue { get; set; } = true;
}

public sealed class QueuedFlashcardReview
{
    public string ClientReviewId { get; set; } = Guid.NewGuid().ToString("N");
    public int FlashcardId { get; set; }
    public int Rating { get; set; }
    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class BatchFlashcardReviewRequest
{
    public List<QueuedFlashcardReview> Reviews { get; set; } = new();
    public string Language { get; set; } = "vi";
}

public sealed class BatchFlashcardReviewItemResult
{
    public string? ClientReviewId { get; set; }
    public int FlashcardId { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int NewBoxLevel { get; set; }
    public int XpEarned { get; set; }
    public bool IsMastered { get; set; }
    public DateTime? NextReviewDate { get; set; }
}

public sealed class BatchFlashcardReviewResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int DuplicateSkippedCount { get; set; }
    public int TotalXpEarned { get; set; }
    public int TotalXp { get; set; }
    public int StreakDays { get; set; }
    public List<BatchFlashcardReviewItemResult> Results { get; set; } = new();
}

public sealed class OfflineSyncFlushResult
{
    public bool Attempted { get; set; }
    public bool IsSuccess { get; set; }
    public int SyncedCount { get; set; }
    public int RemainingCount { get; set; }
    public int XpEarned { get; set; }
    public int TotalXp { get; set; }
    public int StreakDays { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum AtusStreamEventType
{
    Init,
    Chunk,
    Done,
    Error
}

public sealed class AtusStreamEvent
{
    public AtusStreamEventType Type { get; set; }
    public Guid ConversationId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Persisted { get; set; }
}

public sealed class AtusStatusInfo
{
    public bool Success { get; set; }
    public bool IsLiveGemini { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool MobileStreaming { get; set; }
    public string TtsEndpoint { get; set; } = "/api/tts/speak";
    public string Message { get; set; } = string.Empty;
}

public sealed class AtusConversationSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public string TimeGroup { get; set; } = string.Empty;
}

public sealed class AtusChatMessageDto
{
    public long Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? CodeSnippet { get; set; }
    public List<string> SuggestedFollowUps { get; set; } = new();
    public string? ModelUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AtusConversationDetail
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public List<AtusChatMessageDto> Messages { get; set; } = new();
}

public sealed class AtusChatRequestPayload
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Language { get; set; } = "vi";
    public string? CodeContext { get; set; }
    public string CurrentPage { get; set; } = "mobile";
}

public sealed class AtusChatResponsePayload
{
    public bool Success { get; set; }
    public Guid ConversationId { get; set; }
    public string ConversationTitle { get; set; } = string.Empty;
    public string ResponseMarkdown { get; set; } = string.Empty;
    public string? CodeSnippet { get; set; }
    public List<string> SuggestedFollowUps { get; set; } = new();
    public string? ModelUsed { get; set; }
}

public sealed class VoiceEvaluationRequest
{
    public int FlashcardId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Language { get; set; } = "vi";
}

public sealed class VoiceEvaluationResult
{
    public bool IsSuccess { get; set; } = true;
    public int FlashcardId { get; set; }
    public int OverallScore { get; set; }
    public int TechnicalAccuracyScore { get; set; }
    public int ClarityStructureScore { get; set; }
    public int DepthPracticalScore { get; set; }
    public string PerformanceRating { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> MissingPoints { get; set; } = new();
    public string FollowUpQuestion { get; set; } = string.Empty;
    public string ModelAnswerSample { get; set; } = string.Empty;
    public string SpokenSummary { get; set; } = string.Empty;
    public bool IsOfflineEvaluation { get; set; }
}

public sealed class SpeechRecognitionResult
{
    public bool IsSuccess { get; set; }
    public string Transcript { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "vi-VN";
    public string? ErrorMessage { get; set; }
}
