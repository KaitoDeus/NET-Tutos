namespace NET_Tutos.Models.ViewModels;

public class AiTutorChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? CodeContext { get; set; }
    public string? ErrorContext { get; set; }
    public string? CurrentPage { get; set; }
}

public class AiTutorChatResponse
{
    public bool Success { get; set; } = true;
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
