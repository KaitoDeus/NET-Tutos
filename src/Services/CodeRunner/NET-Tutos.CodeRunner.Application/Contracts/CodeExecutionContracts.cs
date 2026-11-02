namespace NET_Tutos.CodeRunner.Application.Contracts;

public class ExecuteScriptRequest
{
    public string Code { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 4000;
}

public class ExecuteScriptResponse
{
    public bool IsSuccess { get; set; }
    public string Output { get; set; } = string.Empty;
    public string? Error { get; set; }
    public long ExecutionTimeMs { get; set; }
    public bool IsTimeout { get; set; }
}

public class EvaluateTestCaseDto
{
    public int Id { get; set; }
    public string InputParameters { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
}

public class EvaluateChallengeRequest
{
    public int ChallengeId { get; set; }
    public string SolutionCode { get; set; } = string.Empty;
    public List<EvaluateTestCaseDto> TestCases { get; set; } = new();
}

public class EvaluateChallengeResponse
{
    public int ChallengeId { get; set; }
    public bool AllPassed { get; set; }
    public int PassedTestsCount { get; set; }
    public int TotalTestsCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? CompileError { get; set; }
    public List<TestCaseResultDto> Details { get; set; } = new();
}

public class TestCaseResultDto
{
    public int TestCaseId { get; set; }
    public string Input { get; set; } = string.Empty;
    public string Expected { get; set; } = string.Empty;
    public string Actual { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public bool IsHidden { get; set; }
    public string? Error { get; set; }
}
