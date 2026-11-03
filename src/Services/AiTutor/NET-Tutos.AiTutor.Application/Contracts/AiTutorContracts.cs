namespace NET_Tutos.AiTutor.Application.Contracts;

public class AiChatRequestDto
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CodeContext { get; set; }
    public string? ErrorContext { get; set; }
    public string? CurrentPage { get; set; }
    public bool IsEnglish { get; set; }
}

public class AiChatResponseDto
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

public class ExplainErrorRequestDto
{
    public string ErrorMessage { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public bool IsEnglish { get; set; }
}

public class ExplainErrorResponseDto
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

public class ChallengeHintRequestDto
{
    public int ChallengeId { get; set; }
    public string? ChallengeTitle { get; set; }
    public string CurrentCode { get; set; } = string.Empty;
    public int HintLevel { get; set; } = 1;
    public bool IsEnglish { get; set; }
}

public class ChallengeHintResponseDto
{
    public bool Success { get; set; } = true;
    public int HintLevel { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CodeClue { get; set; }
    public bool HasNextLevel { get; set; }
}

public class CodeReviewRequestDto
{
    public string SourceCode { get; set; } = string.Empty;
    public bool IsEnglish { get; set; }
}

public class CodeReviewPointDto
{
    public string Category { get; set; } = "Clean Code";
    public string Severity { get; set; } = "info";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CodeReviewResponseDto
{
    public bool Success { get; set; } = true;
    public int Score { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<CodeReviewPointDto> Points { get; set; } = new();
    public string? RefactoredCode { get; set; }
}

public class AnalyzeComplexityRequestDto
{
    public string SourceCode { get; set; } = string.Empty;
    public string? ChallengeTitle { get; set; }
    public bool IsEnglish { get; set; }
}

public class AnalyzeComplexityResponseDto
{
    public bool Success { get; set; } = true;
    public string TimeComplexity { get; set; } = "O(N)";
    public string SpaceComplexity { get; set; } = "O(1)";
    public int EfficiencyScore { get; set; } = 85;
    public string Summary { get; set; } = string.Empty;
    public string DetailedExplanation { get; set; } = string.Empty;
    public string? OptimizationTip { get; set; }
    public string? OptimalComparison { get; set; }
}

public class DiagnoseTestFailureRequestDto
{
    public int ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string InputParameters { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string ActualOutput { get; set; } = string.Empty;
    public int TestIndex { get; set; } = 1;
    public bool IsEnglish { get; set; }
}

public class DiagnoseTestFailureResponseDto
{
    public bool Success { get; set; } = true;
    public string FailureCategory { get; set; } = "Logic Flaw";
    public string DiagnosticTitle { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public string? LikelyFaultyCodeSegment { get; set; }
    public string GuidanceHint { get; set; } = string.Empty;
    public string? SuggestedFixSnippet { get; set; }
}

public class GenerateUnitTestRequestDto
{
    public string SourceCode { get; set; } = string.Empty;
    public string? MethodOrClassName { get; set; }
    public bool IsEnglish { get; set; }
}

public class GenerateUnitTestResponseDto
{
    public bool Success { get; set; } = true;
    public string TestFramework { get; set; } = "xUnit (.NET 10)";
    public string TestCode { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<string> CoveredScenarios { get; set; } = new();
}

public class EvaluateInterviewRequestDto
{
    public int FlashcardId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string CandidateAnswer { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string? BenchmarkAnswer { get; set; }
    public bool IsEnglish { get; set; }
}

public class EvaluateInterviewResponseDto
{
    public bool Success { get; set; } = true;
    public int FlashcardId { get; set; }
    public int OverallScore { get; set; }
    public int TechnicalAccuracyScore { get; set; }
    public int ClarityStructureScore { get; set; }
    public int DepthPracticalScore { get; set; }
    public string PerformanceRating { get; set; } = "Solid Candidate";
    public string Feedback { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> MissingPoints { get; set; } = new();
    public string FollowUpQuestion { get; set; } = string.Empty;
    public string ModelAnswerSample { get; set; } = string.Empty;
}

public class InterviewFollowUpRequestDto
{
    public int FlashcardId { get; set; }
    public string FollowUpQuestion { get; set; } = string.Empty;
    public string CandidateAnswer { get; set; } = string.Empty;
    public string OriginalQuestion { get; set; } = string.Empty;
    public bool IsEnglish { get; set; }
}

public class InterviewFollowUpResponseDto
{
    public bool Success { get; set; } = true;
    public bool IsSatisfactory { get; set; } = true;
    public string Feedback { get; set; } = string.Empty;
    public int BonusScore { get; set; } = 10;
    public string DeepDiveInsight { get; set; } = string.Empty;
}

public class InterviewReportItemDto
{
    public int FlashcardId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string CandidateAnswer { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class InterviewReportRequestDto
{
    public List<InterviewReportItemDto> Items { get; set; } = new();
    public int TimeSpentSeconds { get; set; }
    public bool IsEnglish { get; set; }
}

public class InterviewSkillBreakdownDto
{
    public string SkillName { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Level { get; set; } = "Good";
}

public class InterviewRecommendedLessonDto
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class InterviewReportResponseDto
{
    public bool Success { get; set; } = true;
    public int AverageScore { get; set; }
    public string ReadinessLevel { get; set; } = string.Empty;
    public string ReadinessBadgeClass { get; set; } = "success";
    public string ExecutiveAssessment { get; set; } = string.Empty;
    public List<InterviewSkillBreakdownDto> SkillBreakdowns { get; set; } = new();
    public List<InterviewRecommendedLessonDto> RecommendedLessons { get; set; } = new();
    public int XpAwarded { get; set; }
    public int TimeSpentSeconds { get; set; }
}

public class AiTutorStatusResponseDto
{
    public bool Success { get; set; } = true;
    public bool IsLiveGemini { get; set; }
    public string Provider { get; set; } = "Atus AI Microservice Engine";
    public string Model { get; set; } = "Atus";
    public bool IsAvailable { get; set; } = true;
    public long LatencyMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
