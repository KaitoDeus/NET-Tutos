namespace NET_Tutos.Models.ViewModels;

public class AiTutorChatRequest
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CodeContext { get; set; }
    public string? ErrorContext { get; set; }
    public string? CurrentPage { get; set; }
    public string? AttachedFileName { get; set; }
    public string? AttachedFileContent { get; set; }
}

public class AiTutorChatResponse
{
    public string? ModelUsed { get; set; }
    public bool Success { get; set; } = true;
    public Guid ConversationId { get; set; }
    public string ConversationTitle { get; set; } = string.Empty;
    public string ResponseMarkdown { get; set; } = string.Empty;
    public string? CodeSnippet { get; set; }
    public List<string> SuggestedFollowUps { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AiExplainErrorRequest
{
    public string ErrorMessage { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
}

public class AiExplainErrorResponse
{
    public bool Success { get; set; } = true;
    public string? ErrorCode { get; set; }
    public int? LineNumber { get; set; }
    public string ErrorTitle { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string HowToFix { get; set; } = string.Empty;
    public string? SuggestedFixCode { get; set; }
    public string? ProTip { get; set; }
}

public class AiChallengeHintRequest
{
    public int ChallengeId { get; set; }
    public string CurrentCode { get; set; } = string.Empty;
    public int HintLevel { get; set; } = 1; // 1: Idea, 2: Structure, 3: Pseudocode
}

public class AiChallengeHintResponse
{
    public bool Success { get; set; } = true;
    public int HintLevel { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CodeClue { get; set; }
    public bool HasNextLevel { get; set; }
}

public class AiCodeReviewRequest
{
    public string SourceCode { get; set; } = string.Empty;
}

public class AiCodeReviewResponse
{
    public bool Success { get; set; } = true;
    public int Score { get; set; } // 1 - 100
    public string Summary { get; set; } = string.Empty;
    public List<AiReviewPoint> Points { get; set; } = new();
    public string? RefactoredCode { get; set; }
}

public class AiReviewPoint
{
    public string Category { get; set; } = "Clean Code"; // Performance, Clean Code, Modern C#, Security
    public string Severity { get; set; } = "info"; // info, warning, success
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AiComplexityRequest
{
    public string SourceCode { get; set; } = string.Empty;
    public string? ChallengeTitle { get; set; }
}

public class AiComplexityResponse
{
    public bool Success { get; set; } = true;
    public string TimeComplexity { get; set; } = "O(N)";
    public string SpaceComplexity { get; set; } = "O(1)";
    public int EfficiencyScore { get; set; } = 85; // 1 - 100
    public string Summary { get; set; } = string.Empty;
    public string DetailedExplanation { get; set; } = string.Empty;
    public string? OptimizationTip { get; set; }
    public string? OptimalComparison { get; set; }
}

public class AiDiagnoseTestFailureRequest
{
    public int ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string InputParameters { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string ActualOutput { get; set; } = string.Empty;
    public int TestIndex { get; set; } = 1;
}

public class AiDiagnoseTestFailureResponse
{
    public bool Success { get; set; } = true;
    public string FailureCategory { get; set; } = "Logic Flaw"; // Edge Case, Off-by-one, Logic Flaw, Overflow, Type Mismatch
    public string DiagnosticTitle { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public string? LikelyFaultyCodeSegment { get; set; }
    public string GuidanceHint { get; set; } = string.Empty;
    public string? SuggestedFixSnippet { get; set; }
}

public class AiGenerateUnitTestRequest
{
    public string SourceCode { get; set; } = string.Empty;
    public string? MethodOrClassName { get; set; }
}

public class AiGenerateUnitTestResponse
{
    public bool Success { get; set; } = true;
    public string TestFramework { get; set; } = "xUnit";
    public string TestCode { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<string> CoveredScenarios { get; set; } = new();
}

public class AiInterviewEvaluationRequest
{
    public int FlashcardId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string CandidateAnswer { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
}

public class AiInterviewEvaluationResponse
{
    public bool Success { get; set; } = true;
    public int FlashcardId { get; set; }
    public int OverallScore { get; set; } // 0 - 100
    public int TechnicalAccuracyScore { get; set; } // 0 - 100
    public int ClarityStructureScore { get; set; } // 0 - 100
    public int DepthPracticalScore { get; set; } // 0 - 100
    public string PerformanceRating { get; set; } = "Solid Candidate";
    public string Feedback { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> MissingPoints { get; set; } = new();
    public string FollowUpQuestion { get; set; } = string.Empty;
    public string ModelAnswerSample { get; set; } = string.Empty;
}

public class AiInterviewFollowUpRequest
{
    public int FlashcardId { get; set; }
    public string FollowUpQuestion { get; set; } = string.Empty;
    public string CandidateAnswer { get; set; } = string.Empty;
    public string OriginalQuestion { get; set; } = string.Empty;
}

public class AiInterviewFollowUpResponse
{
    public bool Success { get; set; } = true;
    public bool IsSatisfactory { get; set; } = true;
    public string Feedback { get; set; } = string.Empty;
    public int BonusScore { get; set; } = 10;
    public string DeepDiveInsight { get; set; } = string.Empty;
}

public class AiInterviewReportItem
{
    public int FlashcardId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string CandidateAnswer { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class AiInterviewReportRequest
{
    public List<AiInterviewReportItem> Items { get; set; } = new();
    public int TimeSpentSeconds { get; set; }
}

public class AiInterviewSkillBreakdown
{
    public string SkillName { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Level { get; set; } = "Good";
}

public class AiInterviewRecommendedLesson
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class AiInterviewReportResponse
{
    public bool Success { get; set; } = true;
    public int AverageScore { get; set; }
    public string ReadinessLevel { get; set; } = string.Empty;
    public string ReadinessBadgeClass { get; set; } = "success";
    public string ExecutiveAssessment { get; set; } = string.Empty;
    public List<AiInterviewSkillBreakdown> SkillBreakdowns { get; set; } = new();
    public List<AiInterviewRecommendedLesson> RecommendedLessons { get; set; } = new();
    public int XpAwarded { get; set; }
    public int TimeSpentSeconds { get; set; }
}



public class AiTutorStatusResponse
{
    public bool Success { get; set; }
    public bool IsLiveGemini { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public long LatencyMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
public class AiConversationSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public string TimeGroup { get; set; } = "Hôm nay";
}

public class AiConversationDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AiChatMessageDto> Messages { get; set; } = new();
}

public class AiChatMessageDto
{
    public long Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = "user"; // "user" or "model"
    public string Content { get; set; } = string.Empty;
    public string? CodeSnippet { get; set; }
    public List<string> SuggestedFollowUps { get; set; } = new();
    public string? ModelUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AiCreateConversationRequest
{
    public string? Title { get; set; }
}

public class AiUpdateTitleRequest
{
    public string Title { get; set; } = string.Empty;
}

public class AiTogglePinRequest
{
    public bool IsPinned { get; set; }
}

public class AiCodeCompletionRequest
{
    public string PrefixCode { get; set; } = string.Empty;
    public string? SuffixCode { get; set; }
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public string? Language { get; set; } = "csharp";
    public int? ChallengeId { get; set; }
}

public class AiCodeCompletionResponse
{
    public bool Success { get; set; } = true;
    public string Completion { get; set; } = string.Empty;
    public string? ModelUsed { get; set; }
}

public class AiTutorIndexViewModel
{
    public bool IsAuthenticated { get; set; }
    public string? CurrentUserId { get; set; }
    public string? CurrentUserName { get; set; }
    public string? CurrentUserAvatar { get; set; }
    public List<AiConversationSummaryDto> Conversations { get; set; } = new();
    public AiConversationDetailDto? ActiveConversation { get; set; }
    public Guid? ActiveConversationId => ActiveConversation?.Id;
    public bool IsLiveGeminiConfigured { get; set; }
}


public class AiAdaptiveChallengeRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Intermediate";
}

public class AiAdaptiveChallengeResponse
{
    public bool Success { get; set; } = true;
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ProblemStatementMarkdown { get; set; } = string.Empty;
    public string StarterCode { get; set; } = string.Empty;
    public string SolutionCode { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = "Atus";
}

public class LiveCodingEvaluationRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
}
