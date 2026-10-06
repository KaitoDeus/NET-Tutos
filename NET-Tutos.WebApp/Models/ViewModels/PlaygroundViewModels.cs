using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class PlaygroundIndexViewModel
{
    public List<CodeTemplateItem> Templates { get; set; } = new();
    public List<CodingChallenge> Challenges { get; set; } = new();
    public CodingChallenge? ActiveChallenge { get; set; }
    public CodeSubmission? LastSubmission { get; set; }
}

public class CodeTemplateItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class CodeExecutionRequest
{
    public string Code { get; set; } = string.Empty;
}

public class CodeExecutionResponse
{
    public bool IsSuccess { get; set; }
    public string Output { get; set; } = string.Empty;
    public string? Error { get; set; }
    public long ExecutionTimeMs { get; set; }
    public bool IsTimeout { get; set; }
}

public class ChallengeSubmissionRequest
{
    public int ChallengeId { get; set; }
    public string Code { get; set; } = string.Empty;
}

public class ChallengeSubmissionResponse
{
    public bool AllPassed { get; set; }
    public int PassedTestsCount { get; set; }
    public int TotalTestsCount { get; set; }
    public int XpEarned { get; set; }
    public int TotalUserXp { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? CompileError { get; set; }
    public List<TestCaseEvaluationItem> Details { get; set; } = new();
}

public class TestCaseEvaluationItem
{
    public int TestIndex { get; set; }
    public string Input { get; set; } = string.Empty;
    public string Expected { get; set; } = string.Empty;
    public string Actual { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public bool IsHidden { get; set; }
    public string? Error { get; set; }
}
